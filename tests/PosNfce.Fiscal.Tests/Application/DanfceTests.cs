using System.Runtime.Versioning;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosNfce.Checkout.Application.Contracts;
using PosNfce.Fiscal.Adapters.Out.Certificates;
using PosNfce.Fiscal.Adapters.Out.Persistence;
using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Issuers;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Application.Printing;
using PosNfce.Fiscal.Domain.Documents;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests.Application;

/// <summary>DANFCE built from documents that went through the real signing, saved and read back from the database.</summary>
[SupportedOSPlatform("windows")]
public sealed class DanfceTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("posnfce-danfce-").FullName;
    private readonly FakeSefaz _sefaz = new();
    private readonly FakeReceiptPrinter _printer = new();

    public DanfceTests()
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

    private FiscalTestKit Kit() => new(NewContext, _folder, _sefaz, _printer);

    private GetDanfce NewGetDanfce() => Kit().GetDanfce();

    private async Task SaveIssuerAsync(bool autoIssue = false) =>
        await new SaveIssuer(Kit().Issuers(), new X509CertificateReader(), TimeProvider.System).ExecuteAsync(new SaveIssuerCommand(
            "11222333000181", "0000000000000", "EMPRESA TESTE LTDA", null, "Rua A", "100", null, "Centro", "Belo Horizonte",
            "3106200", "MG", "30110000", null, 1, 1, "1", "TESTE0123456789ABCDEF",
            TestCertificates.Create("11222333000181", DateTimeOffset.Now.AddYears(1)), TestCertificates.Password, autoIssue));

    private async Task<Guid> PendingAsync(string? cpf = null)
    {
        await Kit().Handler().HandleAsync(new SaleCompleted(
            Guid.NewGuid(), DateTimeOffset.Now, 20m, 2m, 18m, 2m,
            [new SoldItem(1, Guid.NewGuid(), "P1", "SEM GTIN", "Refrigerante lata 350ml", "UN", 2m, 10m, 20m,
                "22021000", null, 0, "5102", "102", "49", 0m, "49", 0m, "000", "000001", 0m)],
            [new ReceivedPayment("Cash", 20m, false)],
            cpf));
        return (await new FiscalDocumentRepository(NewContext()).ListRecentAsync(1)).Single().Id;
    }

    private static List<string> Texts(Danfce danfce) => danfce.Lines.Select(l => l.Text).ToList();

    [Fact]
    public async Task Unsigned_document_has_no_danfce_yet()
    {
        await SaveIssuerAsync();
        var id = await PendingAsync();

        await Assert.ThrowsAsync<DomainException>(() => NewGetDanfce().ExecuteAsync(id));
    }

    [Fact]
    public async Task Signed_document_shows_a_preview_with_the_saved_qr_code()
    {
        await SaveIssuerAsync();
        var id = await PendingAsync();
        var prepared = await Kit().Prepare().ExecuteAsync(id);

        var danfce = await NewGetDanfce().ExecuteAsync(id);
        var texts = Texts(danfce);

        Assert.False(danfce.Authorized);
        Assert.StartsWith("https://", danfce.QrCode);
        Assert.Contains(prepared.AccessKey, danfce.QrCode);
        Assert.Matches("^https?://", texts[texts.IndexOf("Consulte pela Chave de Acesso em") + 1]);
        Assert.Contains("PRÉVIA - NOTA AINDA NÃO AUTORIZADA PELA SEFAZ", texts);
        Assert.Contains("CONSUMIDOR NÃO IDENTIFICADO", texts);
        Assert.Contains(string.Join(' ', prepared.AccessKey.Chunk(4).Select(c => new string(c))), string.Join(' ', texts));
        Assert.Contains(texts, t => t.StartsWith("001 P1 " + NfceDraft.HomologationDescription[..20]));
        Assert.Contains(texts, t => t.StartsWith("Desconto R$") && t.EndsWith("2,00"));
        Assert.Contains(texts, t => t.StartsWith("Troco R$") && t.EndsWith("2,00"));
        Assert.Equal(["EMITIDA EM AMBIENTE DE HOMOLOGAÇÃO", "SEM VALOR FISCAL"], texts[^2..]);
        Assert.Single(texts, "[QRCODE]");
    }

    [Fact]
    public async Task Authorized_document_shows_the_protocol_and_the_customer_cpf()
    {
        await SaveIssuerAsync();
        var id = await PendingAsync(cpf: "52998224725");
        _sefaz.Next = new SefazAnswer(SefazOutcome.Authorized, 100, "Autorizado o uso da NF-e", "131260000000001", DateTimeOffset.Now,
            "<nfeProc>simulated</nfeProc>");
        await Kit().Issue().ExecuteAsync(id);

        var danfce = await NewGetDanfce().ExecuteAsync(id);
        var texts = Texts(danfce);

        Assert.True(danfce.Authorized);
        Assert.Contains("Protocolo de autorização: 131260000000001", texts);
        Assert.Contains("CONSUMIDOR - CPF 529.982.247-25", texts);
        Assert.DoesNotContain(texts, t => t.StartsWith("PRÉVIA"));
    }

    [Fact]
    public async Task Left_aligned_lines_fit_an_80_mm_roll()
    {
        await SaveIssuerAsync();
        var id = await PendingAsync();
        await Kit().Prepare().ExecuteAsync(id);

        var danfce = await NewGetDanfce().ExecuteAsync(id);

        Assert.All(
            danfce.Lines.Where(l => l.Style is not (DanfceStyle.Centered or DanfceStyle.CenteredBold)),
            l => Assert.True(l.Text.Length <= Danfce.DefaultColumns, $"\"{l.Text}\" has {l.Text.Length} characters"));
    }

    [Fact]
    public async Task Every_line_fits_a_58_mm_roll_when_the_printer_uses_32_columns()
    {
        await SaveIssuerAsync();
        await Kit().PrinterSettings().SaveAsync(ReceiptPrinterSettings.Default with { PrinterName = "Termica", Columns = 32 });
        var id = await PendingAsync(cpf: "52998224725");
        await Kit().Prepare().ExecuteAsync(id);

        var danfce = await NewGetDanfce().ExecuteAsync(id);

        Assert.Equal(32, danfce.Width);
        Assert.All(danfce.Lines, l => Assert.True(l.Text.Length <= 32, $"\"{l.Text}\" has {l.Text.Length} characters"));
        Assert.Contains(danfce.Lines, l => l.Text.StartsWith("Valor a pagar R$") && l.Text.Length == 32);
    }

    [Fact]
    public async Task Printing_needs_a_configured_printer()
    {
        await SaveIssuerAsync();
        var id = await PendingAsync();
        await Kit().Prepare().ExecuteAsync(id);

        var error = await Assert.ThrowsAsync<DomainException>(() => Kit().PrintDanfce().ExecuteAsync(id));

        Assert.Contains("Impressora", error.Message);
        Assert.Empty(_printer.Printed);
    }

    [Fact]
    public async Task Authorized_sale_is_printed_automatically_when_the_printer_is_set_to()
    {
        await SaveIssuerAsync(autoIssue: true);
        await Kit().PrinterSettings().SaveAsync(ReceiptPrinterSettings.Default with { PrinterName = "Termica", PrintAfterAuthorization = true });
        _sefaz.Next = Authorized();

        await PendingAsync();

        var (receipt, settings) = Assert.Single(_printer.Printed);
        Assert.True(receipt.Authorized);
        Assert.Equal("Termica", settings.PrinterName);
    }

    [Fact]
    public async Task Printer_failure_after_authorization_tells_the_cashier_to_reprint()
    {
        await SaveIssuerAsync(autoIssue: true);
        await Kit().PrinterSettings().SaveAsync(ReceiptPrinterSettings.Default with { PrinterName = "Termica", PrintAfterAuthorization = true });
        _sefaz.Next = Authorized();
        _printer.FailWith = "Impressora sem papel.";

        var error = await Assert.ThrowsAsync<DomainException>(() => PendingAsync());

        Assert.StartsWith("NFC-e autorizada, mas o cupom não foi impresso: Impressora sem papel.", error.Message);
    }

    [Fact]
    public async Task Authorized_sale_is_not_printed_when_automatic_printing_is_off()
    {
        await SaveIssuerAsync(autoIssue: true);
        await Kit().PrinterSettings().SaveAsync(ReceiptPrinterSettings.Default with { PrinterName = "Termica" });
        _sefaz.Next = Authorized();

        await PendingAsync();

        Assert.Empty(_printer.Printed);
    }

    private static SefazAnswer Authorized() =>
        new(SefazOutcome.Authorized, 100, "Autorizado o uso da NF-e", "131260000000001", DateTimeOffset.Now, "<nfeProc>simulated</nfeProc>");
}
