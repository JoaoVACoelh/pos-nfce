using PosNfce.Fiscal.Application.Ports;

namespace PosNfce.Fiscal.Application.Printing;

public sealed class SaveReceiptPrinterSettings(IReceiptPrinterSettingsStore store)
{
    public async Task ExecuteAsync(ReceiptPrinterSettings settings, CancellationToken cancellationToken = default) =>
        await store.SaveAsync(settings.Validated(), cancellationToken);
}
