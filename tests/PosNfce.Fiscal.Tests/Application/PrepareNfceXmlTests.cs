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
using PosNfce.Fiscal.Application.Taxes;
using PosNfce.Fiscal.Domain.Taxes;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests.Application;

/// <summary>
/// End to end, offline: a completed sale becomes a signed NFC-e XML validated against the official schema by the real
/// library (Unimake.DFe), with a self-signed certificate. Fictitious data; nothing is sent to SEFAZ.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class PrepareNfceXmlTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("posnfce-xml-").FullName;
    private readonly string _database;

    public PrepareNfceXmlTests()
    {
        _database = Path.Combine(_folder, "pos.db");
        using var db = NewContext();
        db.Database.Migrate();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }

    private FiscalDbContext NewContext() => new(new DbContextOptionsBuilder<FiscalDbContext>()
        .UseSqlite($"Data Source={_database}", sqlite => sqlite.MigrationsHistoryTable(FiscalDbContext.MigrationsHistoryTable))
        .Options);

    private PrepareNfceXml NewPrepare(TimeProvider? clock = null) => new(
        new FiscalDocumentRepository(NewContext()),
        new IssuerRepository(NewContext(), new DpapiSecretProtector()),
        new SqliteDocumentNumbering(NewContext()),
        new FixedApproximateRates(),
        new IbsCbsRateRepository(NewContext()),
        new UnimakeNfceIssuer(),
        new FileXmlStore(_folder),
        clock ?? TimeProvider.System);

    private async Task<Guid> PendingDocumentAsync(string ibsCbsCst = "000", decimal reduction = 0m)
    {
        await new SaveIssuer(new IssuerRepository(NewContext(), new DpapiSecretProtector()), new X509CertificateReader(), TimeProvider.System)
            .ExecuteAsync(new SaveIssuerCommand("11222333000181", "0000000000000", "EMPRESA TESTE LTDA", "Mercadinho Teste", "Rua A", "100", null,
                "Centro", "Belo Horizonte", "3106200", "MG", "30110000", "3130000000", 1, 1, "1", "TESTE0123456789ABCDEF",
                TestCertificates.Create("11222333000181", DateTimeOffset.Now.AddYears(1)), TestCertificates.Password));

        var sale = new SaleCompleted(Guid.NewGuid(), DateTimeOffset.Now, 15.98m, 0.98m, 15m, 5m,
            [
                new SoldItem(1, Guid.NewGuid(), "REF", "SEM GTIN", "Refrigerante", "UN", 2m, 5.49m, 10.98m,
                    "22021000", null, 0, "5405", "500", "49", 0m, "49", 0m, ibsCbsCst, "000001", reduction),
                new SoldItem(2, Guid.NewGuid(), "PAO", "SEM GTIN", "Pão francês", "KG", 0.5m, 10m, 5m,
                    "19059090", null, 0, "5102", "102", "04", 0m, "04", 0m, ibsCbsCst, "000001", reduction),
            ],
            [new ReceivedPayment("DebitCard", 10m, false), new ReceivedPayment("Cash", 10m, false)]);
        await new SaleCompletedHandler(new RegisterSaleDocument(new FiscalDocumentRepository(NewContext()))).HandleAsync(sale);

        return (await new FiscalDocumentRepository(NewContext()).ListRecentAsync(1)).Single().Id;
    }

    private static XmlDocument Load(string path)
    {
        var xml = new XmlDocument();
        xml.Load(path);
        return xml;
    }

    private static string Tag(XmlDocument xml, string name) => xml.GetElementsByTagName(name)[0]?.InnerText ?? "";

    [Fact]
    public async Task Pending_document_becomes_a_signed_schema_valid_xml_file()
    {
        var id = await PendingDocumentAsync();

        var prepared = await NewPrepare().ExecuteAsync(id);

        Assert.Equal((1, 1L), (prepared.Series, prepared.Number));
        // cUF 31 (MG) + AAMM + CNPJ + model 65 + series 001 + number 000000001 + tpEmis 1 + cNF (8) + check digit
        Assert.Matches("^31\\d{4}11222333000181650010000000011\\d{9}$", prepared.AccessKey);
        Assert.True(File.Exists(prepared.XmlPath));

        var xml = Load(prepared.XmlPath);
        Assert.NotNull(xml.GetElementsByTagName("Signature")[0]);
        Assert.Equal("2", Tag(xml, "tpAmb"));
        Assert.Equal("NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL", Tag(xml, "xProd"));
        Assert.Equal("15.00", Tag(xml, "vNF"));
        // 0,98 split by item (10,98 and 5,00 → 0,68 + 0,30) and the total in ICMSTot.
        var itemDiscounts = xml.GetElementsByTagName("prod").Cast<XmlElement>().Select(p => p.GetElementsByTagName("vDesc")[0]!.InnerText);
        Assert.Equal(["0.68", "0.30"], itemDiscounts);
        Assert.Equal("0.98", ((XmlElement)xml.GetElementsByTagName("ICMSTot")[0]!).GetElementsByTagName("vDesc")[0]!.InnerText);
        Assert.Equal("5.00", Tag(xml, "vTroco"));
        Assert.Contains("Trib aprox", Tag(xml, "infCpl"));
        Assert.Equal(prepared.AccessKey, (await new FiscalDocumentRepository(NewContext()).FindAsync(id))!.AccessKey);
    }

    [Fact]
    public async Task Csosn_and_pis_groups_and_the_card_group_are_written()
    {
        var prepared = await NewPrepare().ExecuteAsync(await PendingDocumentAsync());
        var xml = Load(prepared.XmlPath);

        Assert.NotNull(xml.GetElementsByTagName("ICMSSN500")[0]);
        Assert.NotNull(xml.GetElementsByTagName("ICMSSN102")[0]);
        Assert.NotNull(xml.GetElementsByTagName("PISOutr")[0]);
        Assert.NotNull(xml.GetElementsByTagName("PISNT")[0]);
        Assert.Equal("2", Tag(xml, "tpIntegra"));
        Assert.Null(xml.GetElementsByTagName("IBSCBS")[0]); // 2026, Simples Nacional
    }

    [Fact]
    public async Task Preparing_again_keeps_the_same_number()
    {
        var id = await PendingDocumentAsync();

        var first = await NewPrepare().ExecuteAsync(id);
        var second = await NewPrepare().ExecuteAsync(id);

        Assert.Equal(first.Number, second.Number);
    }

    [Fact]
    public async Task From_2027_the_ibs_cbs_group_is_written_and_passes_the_schema()
    {
        var id = await PendingDocumentAsync(ibsCbsCst: "200", reduction: 60m);
        await new RegisterIbsCbsRates(new IbsCbsRateRepository(NewContext()))
            .ExecuteAsync(new RegisterIbsCbsRatesCommand(new DateOnly(2027, 1, 1), 0.05m, 0.05m, 8.8m, "TESTE"));
        var january2027 = new FixedClock(new DateTimeOffset(2027, 1, 10, 13, 0, 0, TimeSpan.Zero));

        var prepared = await NewPrepare(january2027).ExecuteAsync(id);
        var xml = Load(prepared.XmlPath);

        Assert.NotNull(xml.GetElementsByTagName("IBSCBS")[0]);
        var ibsCbs = (XmlElement)xml.GetElementsByTagName("IBSCBS")[0]!;
        Assert.Equal("200", ibsCbs.GetElementsByTagName("CST")[0]!.InnerText);
        Assert.Equal(60m, decimal.Parse(Tag(xml, "pRedAliq"), System.Globalization.CultureInfo.InvariantCulture));
        Assert.NotNull(xml.GetElementsByTagName("IBSCBSTot")[0]);
    }

    [Fact]
    public async Task Issuer_without_certificate_gives_a_message()
    {
        await new SaveIssuer(new IssuerRepository(NewContext(), new DpapiSecretProtector()), new X509CertificateReader(), TimeProvider.System)
            .ExecuteAsync(new SaveIssuerCommand("11222333000181", "0000000000000", "EMPRESA TESTE", null, "Rua A", "1", null,
                "Centro", "Belo Horizonte", "3106200", "MG", "30110000", null, 1, 1, null, null, null, null));

        var error = await Assert.ThrowsAsync<DomainException>(() => NewPrepare().ExecuteAsync(Guid.NewGuid()));
        Assert.Contains("certificado", error.Message);
    }

    /// <summary>Fictitious IBPT rates for every NCM.</summary>
    private sealed class FixedApproximateRates : IApproximateTaxRates
    {
        public Task<ApproximateRatesWithSource?> FindAsync(string ncm, int origin, CancellationToken cancellationToken = default) =>
            Task.FromResult<ApproximateRatesWithSource?>(new(new ApproximateRates(13.45m, 18m, 0m), "IBPT TESTE"));
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
