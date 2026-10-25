using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Documents;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Application.Documents;

/// <summary>A completed sale as Fiscal receives it. Mapped from Checkout's event by the inbound adapter.</summary>
public sealed record RegisterSaleDocumentCommand(
    Guid SaleId,
    DateTimeOffset SoldAt,
    decimal Discount,
    decimal Total,
    decimal Change,
    IReadOnlyList<SaleDocumentItem> Items,
    IReadOnlyList<SaleDocumentPayment> Payments);

public sealed record SaleDocumentItem(
    int Line, string Code, string Gtin, string Description, string Unit, decimal Quantity, decimal UnitPrice, decimal Total,
    string Ncm, string? Cest, int Origin, string Cfop, string Csosn,
    string PisCst, decimal PisRate, string CofinsCst, decimal CofinsRate, string IbsCbsCst, string ClassTrib);

public sealed record SaleDocumentPayment(string Method, decimal Amount, bool Integrated);

/// <summary>
/// Creates the pending NFC-e of a completed sale. Receiving the same sale twice creates nothing new,
/// so a retried event never produces a second document.
/// </summary>
public sealed class RegisterSaleDocument(IFiscalDocumentRepository documents)
{
    public async Task<Guid?> ExecuteAsync(RegisterSaleDocumentCommand command, CancellationToken cancellationToken = default)
    {
        if (await documents.ExistsForSaleAsync(command.SaleId, cancellationToken))
        {
            return null;
        }

        var document = FiscalDocument.FromSale(
            command.SaleId,
            command.SoldAt,
            command.Items.Select(i => new FiscalItem(
                i.Line, i.Code, i.Gtin, i.Description, i.Unit, i.Quantity, new Money(i.UnitPrice), new Money(i.Total), Money.Zero,
                i.Ncm, i.Cest, i.Origin, i.Cfop, i.Csosn, i.PisCst, i.PisRate, i.CofinsCst, i.CofinsRate, i.IbsCbsCst, i.ClassTrib)).ToList(),
            new Money(command.Discount),
            new Money(command.Total),
            new Money(command.Change),
            command.Payments.Select(p => new FiscalPayment(p.Method, new Money(p.Amount), p.Integrated)).ToList());

        await documents.AddAsync(document, cancellationToken);
        return document.Id;
    }
}
