using System.Runtime.Versioning;
using PosNfce.Fiscal.Adapters.In;
using PosNfce.Fiscal.Adapters.Out.Files;
using PosNfce.Fiscal.Adapters.Out.Persistence;
using PosNfce.Fiscal.Adapters.Out.Security;
using PosNfce.Fiscal.Adapters.Out.Sefaz;
using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Application.Printing;
using PosNfce.Fiscal.Domain.Documents;
using PosNfce.Fiscal.Domain.Issuers;
using PosNfce.Fiscal.Domain.Taxes;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests;

/// <summary>Real signing and schema validation (Unimake.DFe); simulated SEFAZ answer.</summary>
[SupportedOSPlatform("windows")]
internal sealed class FakeSefaz : IFiscalDocumentIssuer
{
    private readonly UnimakeNfceIssuer _real = new();

    public SefazAnswer Next { get; set; } = new(SefazOutcome.NotSent, null, "simulated SEFAZ not configured", null, null, null);
    public int Calls { get; private set; }

    public SignedNfce Sign(NfceDraft draft, IssuerCertificate certificate, Csc csc) => _real.Sign(draft, certificate, csc);

    public Task<SefazAnswer> SendAsync(
        SignedNfce signed, IssuerCertificate certificate, IssuerProfile issuer, bool homologation, CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(Next);
    }
}

internal sealed class NoApproximateRates : IApproximateTaxRates
{
    public Task<ApproximateRatesWithSource?> FindAsync(string ncm, int origin, CancellationToken cancellationToken = default) =>
        Task.FromResult<ApproximateRatesWithSource?>(null);
}

/// <summary>Keeps what would have been printed; can be told to fail like an unplugged printer.</summary>
internal sealed class FakeReceiptPrinter : IReceiptPrinter
{
    public List<(Danfce Receipt, ReceiptPrinterSettings Settings)> Printed { get; } = [];
    public string? FailWith { get; set; }

    public Task PrintAsync(Danfce receipt, ReceiptPrinterSettings settings, CancellationToken cancellationToken = default)
    {
        if (FailWith is not null)
        {
            throw new DomainException(FailWith);
        }

        Printed.Add((receipt, settings));
        return Task.CompletedTask;
    }
}

/// <summary>Builds the Fiscal use cases and the SaleCompleted handler over one test database, with a simulated SEFAZ.</summary>
[SupportedOSPlatform("windows")]
internal sealed class FiscalTestKit(Func<FiscalDbContext> newContext, string xmlFolder, FakeSefaz? sefaz = null, FakeReceiptPrinter? printer = null)
{
    public FakeSefaz Sefaz { get; } = sefaz ?? new FakeSefaz();
    public FakeReceiptPrinter Printer { get; } = printer ?? new FakeReceiptPrinter();

    public ReceiptPrinterSettingsStore PrinterSettings() => new(newContext());

    public GetDanfce GetDanfce() => new(new FiscalDocumentRepository(newContext()), Issuers(), PrinterSettings());

    public PrintDanfce PrintDanfce() => new(GetDanfce(), PrinterSettings(), Printer);

    public IssuerRepository Issuers() => new(newContext(), new DpapiSecretProtector());

    public PrepareNfceXml Prepare(TimeProvider? clock = null, IApproximateTaxRates? rates = null) => new(
        new FiscalDocumentRepository(newContext()), Issuers(), new SqliteDocumentNumbering(newContext()),
        rates ?? new NoApproximateRates(), new IbsCbsRateRepository(newContext()), Sefaz, new FileXmlStore(xmlFolder),
        clock ?? TimeProvider.System);

    public IssueNfce Issue() =>
        new(Prepare(), new FiscalDocumentRepository(newContext()), Issuers(), Sefaz, new FileXmlStore(xmlFolder));

    public SaleCompletedHandler Handler() =>
        new(new RegisterSaleDocument(new FiscalDocumentRepository(newContext())), Issuers(), Issue(), PrinterSettings(), PrintDanfce());
}
