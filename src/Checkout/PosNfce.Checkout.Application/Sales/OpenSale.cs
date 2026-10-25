using PosNfce.Checkout.Application.Ports;
using PosNfce.Checkout.Domain.Sales;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Application.Sales;

internal static class OpenSale
{
    public static async Task<Sale> LoadAsync(ISaleRepository sales, CancellationToken cancellationToken) =>
        await sales.GetOpenAsync(cancellationToken) ?? throw new DomainException("Não há venda em andamento.");
}
