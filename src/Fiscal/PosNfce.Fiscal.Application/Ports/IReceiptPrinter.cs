using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Printing;

namespace PosNfce.Fiscal.Application.Ports;

/// <summary>Prints a receipt on the computer's printer. Failures come back as <see cref="SharedKernel.DomainException"/> with the reason.</summary>
public interface IReceiptPrinter
{
    Task PrintAsync(Danfce receipt, ReceiptPrinterSettings settings, CancellationToken cancellationToken = default);
}
