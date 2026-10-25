using PosNfce.Checkout.Application.Contracts;
using PosNfce.Checkout.Application.Ports;
using PosNfce.Checkout.Domain.Sales;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Application.Sales;

/// <summary><paramref name="AfterSaleWarning"/>: set when the sale closed but a reaction to it (e.g. the NFC-e) failed.</summary>
public sealed record CompletedSale(Guid SaleId, decimal Total, decimal Change, string? AfterSaleWarning = null);

/// <summary>
/// Closes a fully paid sale and publishes <see cref="SaleCompleted"/>. The sale is saved first: if a handler
/// fails, the sale stays recorded and the cashier is warned, instead of the screen breaking with money already taken.
/// </summary>
public sealed class CompleteSale(ISaleRepository sales, IEventPublisher events, TimeProvider clock)
{
    public async Task<CompletedSale> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var sale = await OpenSale.LoadAsync(sales, cancellationToken);
        var now = BrazilTime.Now(clock);

        sale.Complete(now);
        await sales.SaveAsync(sale, cancellationToken);

        string? warning = null;
        try
        {
            await events.PublishAsync(ToEvent(sale, now), cancellationToken);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            // The customer already paid: the sale can't be undone because of a later step. Stage 4 adds retries.
            warning = $"A venda foi registrada, mas a nota fiscal não: {error.Message}";
        }

        return new CompletedSale(sale.Id, sale.Total.Amount, sale.Change.Amount, warning);
    }

    private static SaleCompleted ToEvent(Sale sale, DateTimeOffset now) => new(
        sale.Id,
        now,
        sale.Subtotal.Amount,
        sale.Discount.Amount,
        sale.Total.Amount,
        sale.Change.Amount,
        sale.Items.Select(i => new SoldItem(
            i.Line, i.ProductId, i.Code, i.Gtin, i.Description, i.Unit, i.Quantity, i.UnitPrice.Amount, i.Total.Amount,
            i.Tax.Ncm, i.Tax.Cest, i.Tax.Origin, i.Tax.Cfop, i.Tax.Csosn,
            i.Tax.PisCst, i.Tax.PisRate, i.Tax.CofinsCst, i.Tax.CofinsRate, i.Tax.IbsCbsCst, i.Tax.ClassTrib, i.Tax.IbsCbsReduction)).ToList(),
        sale.Payments.Select(p => new ReceivedPayment(p.Method.ToString(), p.Amount.Amount, p.Integrated)).ToList(),
        sale.CustomerCpf?.Value);
}
