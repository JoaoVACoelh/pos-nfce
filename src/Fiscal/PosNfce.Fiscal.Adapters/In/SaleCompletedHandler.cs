using PosNfce.Checkout.Application.Contracts;
using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Application.Printing;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Adapters.In;

/// <summary>
/// Inbound adapter: translates Checkout's <see cref="SaleCompleted"/> into Fiscal's own command, so Fiscal's
/// application layer never depends on Checkout. When the issuer has automatic issuing on, the NFC-e is sent right away;
/// if it isn't authorized, the exception carries the reason and the checkout shows it to the cashier. Once authorized, the
/// DANFCE is printed when the printer is set to print automatically.
/// </summary>
public sealed class SaleCompletedHandler(
    RegisterSaleDocument registerDocument, IIssuerRepository issuers, IssueNfce issueNfce,
    IReceiptPrinterSettingsStore printerSettings, PrintDanfce printDanfce)
    : IEventHandler<SaleCompleted>
{
    public async Task HandleAsync(SaleCompleted sale, CancellationToken cancellationToken = default)
    {
        var documentId = await registerDocument.ExecuteAsync(new RegisterSaleDocumentCommand(
            sale.SaleId,
            sale.OccurredAt,
            sale.Discount,
            sale.Total,
            sale.Change,
            sale.Items.Select(i => new SaleDocumentItem(
                i.Line, i.Code, i.Gtin, i.Description, i.Unit, i.Quantity, i.UnitPrice, i.Total,
                i.Ncm, i.Cest, i.Origin, i.Cfop, i.Csosn, i.PisCst, i.PisRate, i.CofinsCst, i.CofinsRate, i.IbsCbsCst, i.ClassTrib, i.IbsCbsReduction)).ToList(),
            sale.Payments.Select(p => new SaleDocumentPayment(p.Method, p.Amount, p.Integrated)).ToList(),
            sale.CustomerCpf),
            cancellationToken);

        if (documentId is null || (await issuers.GetAsync(cancellationToken))?.AutoIssue != true)
        {
            return;
        }

        var result = await issueNfce.ExecuteAsync(documentId.Value, cancellationToken);
        if (!result.Authorized)
        {
            throw new DomainException(result.Message);
        }

        if (!(await printerSettings.GetAsync(cancellationToken)).PrintAfterAuthorization)
        {
            return;
        }

        try
        {
            await printDanfce.ExecuteAsync(documentId.Value, cancellationToken);
        }
        catch (DomainException error)
        {
            throw new DomainException($"NFC-e autorizada, mas o cupom não foi impresso: {error.Message} Reimprima em \"Notas fiscais\".");
        }
    }
}
