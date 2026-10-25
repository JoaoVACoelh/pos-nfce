using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Application.Printing;

/// <summary>Prints the DANFCE of a signed or authorized document on the configured printer.</summary>
public sealed class PrintDanfce(GetDanfce getDanfce, IReceiptPrinterSettingsStore settings, IReceiptPrinter printer)
{
    public async Task ExecuteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var current = await settings.GetAsync(cancellationToken);
        if (!current.IsConfigured)
        {
            throw new DomainException("Configure a impressora na tela \"Impressora\" antes de imprimir.");
        }

        var danfce = await getDanfce.ExecuteAsync(documentId, cancellationToken);
        await printer.PrintAsync(danfce, current, cancellationToken);
    }
}
