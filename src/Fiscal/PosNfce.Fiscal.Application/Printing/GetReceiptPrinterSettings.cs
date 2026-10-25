using PosNfce.Fiscal.Application.Ports;

namespace PosNfce.Fiscal.Application.Printing;

public sealed class GetReceiptPrinterSettings(IReceiptPrinterSettingsStore store)
{
    public Task<ReceiptPrinterSettings> ExecuteAsync(CancellationToken cancellationToken = default) => store.GetAsync(cancellationToken);
}
