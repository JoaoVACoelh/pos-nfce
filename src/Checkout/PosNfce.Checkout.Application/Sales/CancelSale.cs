using PosNfce.Checkout.Application.Ports;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Application.Sales;

public sealed class CancelSale(ISaleRepository sales, TimeProvider clock)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var sale = await OpenSale.LoadAsync(sales, cancellationToken);
        sale.Cancel(BrazilTime.Now(clock));
        await sales.SaveAsync(sale, cancellationToken);
    }
}
