using PosNfce.Fiscal.Application.Printing;

namespace PosNfce.Fiscal.Application.Ports;

/// <summary>One printer per installation, like the issuer.</summary>
public interface IReceiptPrinterSettingsStore
{
    Task<ReceiptPrinterSettings> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ReceiptPrinterSettings settings, CancellationToken cancellationToken = default);
}
