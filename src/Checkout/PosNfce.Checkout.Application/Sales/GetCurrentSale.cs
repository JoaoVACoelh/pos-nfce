using PosNfce.Checkout.Application.Ports;

namespace PosNfce.Checkout.Application.Sales;

public sealed class GetCurrentSale(ISaleRepository sales)
{
    public async Task<SaleView> ExecuteAsync(CancellationToken cancellationToken = default) =>
        SaleView.From(await sales.GetOpenAsync(cancellationToken));
}
