using System.Runtime.Versioning;
using System.Xml;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosNfce.Checkout.Application.Contracts;
using PosNfce.Fiscal.Adapters.In;
using PosNfce.Fiscal.Adapters.Out.Certificates;
using PosNfce.Fiscal.Adapters.Out.Files;
using PosNfce.Fiscal.Adapters.Out.Persistence;
using PosNfce.Fiscal.Adapters.Out.Security;
using PosNfce.Fiscal.Adapters.Out.Sefaz;
using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Issuers;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Documents;
using PosNfce.Fiscal.Domain.Issuers;
using PosNfce.Fiscal.Domain.Taxes;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests.Application;

/// <summary>
/// Sending to SEFAZ with a simulated SEFAZ: the XML is really built, signed and validated by Unimake.DFe;
/// only the answer is fake. Real sending can only be tested with the store's real certificate and CSC.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class IssueNfceTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("posnfce-issue-").FullName;
    private readonly FakeSefaz _sefaz = new();

    public IssueNfceTests()
    {
        using var db = NewContext();
        db.Database.Migrate();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }

    private FiscalDbContext NewContext() => new(new DbContextOptionsBuilder<FiscalDbContext>()
        .UseSqlite($"Data Source={Path.Combine(_folder, "pos.db")}", sqlite => sqlite.MigrationsHistoryTable(FiscalDbContext.MigrationsHistoryTable))
        .Options);

    private FiscalTestKit Kit() => new(NewContext, _folder, _sefaz);

    private IssuerRepository Issuers() => Kit().Issuers();

    private IssueNfce NewIssue() => Kit().Issue();

    private SaleCompletedHandler NewHandler() => Kit().Handler();

    private async Task SaveIssuerAsync(bool autoIssue = false) =>
        await new SaveIssuer(Issuers(), new FiscalDocumentRepository(NewContext()), new X509CertificateReader(), TimeProvider.System).ExecuteAsync(new SaveIssuerCommand(
            "11222333000181", "0000000000000", "EMPRESA TESTE LTDA", null, "Rua A", "100", null, "Centro", "Belo Horizonte",
            "3106200", "MG", "30110000", null, 1, 1, "1", "TESTE0123456789ABCDEF",
            TestCertificates.Create("11222333000181", DateTimeOffset.Now.AddYears(1)), TestCertificates.Password, autoIssue));

    private static SaleCompleted Sale(string? cpf = null) => new(
        Guid.NewGuid(), DateTimeOffset.Now, 10m, 0m, 10m, 0m,
        [new SoldItem(1, Guid.NewGuid(), "P1", "SEM GTIN", "Produto", "UN", 1m, 10m, 10m,
            "22021000", null, 0, "5102", "102", "49", 0m, "49", 0m, "000", "000001", 0m)],
        [new ReceivedPayment("Cash", 10m, false)],
        cpf);

    private async Task<Guid> PendingAsync(string? cpf = null)
    {
        await NewHandler().HandleAsync(Sale(cpf));
        return (await new FiscalDocumentRepository(NewContext()).ListRecentAsync(1)).Single().Id;
    }

    private Task<FiscalDocument?> Find(Guid id) => new FiscalDocumentRepository(NewContext()).FindAsync(id);

    [Fact]
    public async Task Authorized_document_keeps_the_protocol_and_the_authorized_xml()
    {
        await SaveIssuerAsync();
        var id = await PendingAsync();
        _sefaz.Next = Answer(SefazOutcome.Authorized, 100, "Autorizado o uso da NF-e", "131260000000001");

        var result = await NewIssue().ExecuteAsync(id);

        Assert.True(result.Authorized);
        var document = await Find(id);
        Assert.Equal((FiscalDocumentStatus.Authorized, "131260000000001", 100), (document!.Status, document.Protocol, document.LastStatusCode));
        Assert.Single(Directory.GetFiles(_folder, "*-procNFe.xml", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Rejected_document_keeps_its_number_and_can_be_sent_again()
    {
        await SaveIssuerAsync();
        var id = await PendingAsync();
        _sefaz.Next = Answer(SefazOutcome.Rejected, 539, "Duplicidade de NF-e com diferença na Chave de Acesso");

        var rejected = await NewIssue().ExecuteAsync(id);
        Assert.Equal(FiscalDocumentStatus.Rejected, (await Find(id))!.Status);
        Assert.Contains("539", rejected.Message);

        _sefaz.Next = Answer(SefazOutcome.Authorized, 100, "Autorizado o uso da NF-e", "131260000000002");
        var authorized = await NewIssue().ExecuteAsync(id);

        Assert.Equal(rejected.Number, authorized.Number);
        Assert.Equal(FiscalDocumentStatus.Authorized, (await Find(id))!.Status);
    }

    [Fact]
    public async Task Unreachable_sefaz_leaves_the_document_pending_with_the_reason()
    {
        await SaveIssuerAsync();
        var id = await PendingAsync();
        _sefaz.Next = Answer(SefazOutcome.NotSent, null, "Tempo limite esgotado");

        var result = await NewIssue().ExecuteAsync(id);

        Assert.False(result.Authorized);
        var document = await Find(id);
        Assert.Equal((FiscalDocumentStatus.Pending, "Tempo limite esgotado"), (document!.Status, document.LastMessage));
    }

    [Fact]
    public async Task Authorized_document_can_not_be_issued_twice()
    {
        await SaveIssuerAsync();
        var id = await PendingAsync();
        _sefaz.Next = Answer(SefazOutcome.Authorized, 100, "Autorizado o uso da NF-e", "131260000000003");
        await NewIssue().ExecuteAsync(id);

        await Assert.ThrowsAsync<DomainException>(() => NewIssue().ExecuteAsync(id));
    }

    [Fact]
    public async Task Customer_cpf_goes_in_the_dest_group_and_passes_the_schema()
    {
        await SaveIssuerAsync();
        var id = await PendingAsync(cpf: "52998224725");
        _sefaz.Next = Answer(SefazOutcome.NotSent, null, "sem rede");

        await NewIssue().ExecuteAsync(id);

        var file = Directory.GetFiles(_folder, "*-assinada.xml", SearchOption.AllDirectories).Single();
        var xml = new XmlDocument();
        xml.Load(file);
        var dest = (XmlElement)xml.GetElementsByTagName("dest")[0]!;
        Assert.Equal("52998224725", dest.GetElementsByTagName("CPF")[0]!.InnerText);
        Assert.Equal("9", dest.GetElementsByTagName("indIEDest")[0]!.InnerText);
    }

    [Fact]
    public async Task With_auto_issue_the_sale_is_sent_when_it_closes()
    {
        await SaveIssuerAsync(autoIssue: true);
        _sefaz.Next = Answer(SefazOutcome.Authorized, 100, "Autorizado o uso da NF-e", "131260000000004");

        await NewHandler().HandleAsync(Sale());

        Assert.Equal(FiscalDocumentStatus.Authorized, (await new FiscalDocumentRepository(NewContext()).ListRecentAsync(1)).Single().Status);
    }

    [Fact]
    public async Task With_auto_issue_a_rejection_reaches_the_checkout_as_an_error()
    {
        await SaveIssuerAsync(autoIssue: true);
        _sefaz.Next = Answer(SefazOutcome.Rejected, 778, "Informado NCM inexistente");

        var error = await Assert.ThrowsAsync<DomainException>(() => NewHandler().HandleAsync(Sale()));

        Assert.Contains("778", error.Message);
    }

    [Fact]
    public async Task Without_auto_issue_the_sale_only_becomes_a_pending_document()
    {
        await SaveIssuerAsync(autoIssue: false);

        await NewHandler().HandleAsync(Sale());

        Assert.Equal(0, _sefaz.Calls);
    }

    [Fact]
    public void Auto_issue_needs_certificate_and_csc()
    {
        var issuer = Issuer.Register(Domain.IssuerTests.Profile(), 1, 1);

        Assert.Throws<DomainException>(() => issuer.SetAutoIssue(true));
    }

    private static SefazAnswer Answer(SefazOutcome outcome, int? code, string message, string? protocol = null) =>
        new(outcome, code, message, protocol, protocol is null ? null : DateTimeOffset.Now,
            outcome == SefazOutcome.Authorized ? "<nfeProc>simulated</nfeProc>" : null);
}
