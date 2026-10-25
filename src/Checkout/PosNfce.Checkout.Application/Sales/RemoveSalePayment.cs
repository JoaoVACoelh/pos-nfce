using PosNfce.Checkout.Application.Ports;

namespace PosNfce.Checkout.Application.Sales;

public sealed class RemoveSalePayment(ISaleRepository sales)
{
    public async Task<SaleView> ExecuteAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var sale = await OpenSale.LoadAsync(sales, cancellationToken);
        sale.RemovePayment(paymentId);
        await sales.SaveAsync(sale, cancellationToken);
        return SaleView.From(sale);
    }
}
