using PosNfce.Checkout.Application.Contracts;
using PosNfce.Fiscal.Application.Documents;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Adapters.In;

/// <summary>
/// Inbound adapter: translates Checkout's <see cref="SaleCompleted"/> into Fiscal's own command, so Fiscal's
/// application layer never depends on Checkout. Issuing at SEFAZ is added here in Stage 3.
/// </summary>
public sealed class SaleCompletedHandler(RegisterSaleDocument registerDocument) : IEventHandler<SaleCompleted>
{
    public Task HandleAsync(SaleCompleted sale, CancellationToken cancellationToken = default) =>
        registerDocument.ExecuteAsync(new RegisterSaleDocumentCommand(
            sale.SaleId,
            sale.OccurredAt,
            sale.Discount,
            sale.Total,
            sale.Change,
            sale.Items.Select(i => new SaleDocumentItem(
                i.Line, i.Code, i.Gtin, i.Description, i.Unit, i.Quantity, i.UnitPrice, i.Total,
                i.Ncm, i.Cest, i.Origin, i.Cfop, i.Csosn, i.PisCst, i.PisRate, i.CofinsCst, i.CofinsRate, i.IbsCbsCst, i.ClassTrib)).ToList(),
            sale.Payments.Select(p => new SaleDocumentPayment(p.Method, p.Amount, p.Integrated)).ToList()),
            cancellationToken);
}
