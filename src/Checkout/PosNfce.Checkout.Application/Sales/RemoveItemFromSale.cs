using PosNfce.Checkout.Application.Ports;

namespace PosNfce.Checkout.Application.Sales;

public sealed class RemoveItemFromSale(ISaleRepository sales)
{
    public async Task<SaleView> ExecuteAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var sale = await OpenSale.LoadAsync(sales, cancellationToken);
        sale.RemoveItem(itemId);
        await sales.SaveAsync(sale, cancellationToken);
        return SaleView.From(sale);
    }
}
