using PosNfce.Checkout.Application.Ports;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Application.Sales;

public sealed class ApplySaleDiscount(ISaleRepository sales)
{
    public async Task<SaleView> ExecuteAsync(decimal amount, CancellationToken cancellationToken = default)
    {
        var sale = await OpenSale.LoadAsync(sales, cancellationToken);
        sale.ApplyDiscount(new Money(amount));
        await sales.SaveAsync(sale, cancellationToken);
        return SaleView.From(sale);
    }
}
