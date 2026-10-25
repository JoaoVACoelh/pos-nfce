using PosNfce.Checkout.Domain.Sales;

namespace PosNfce.Checkout.Application.Ports;

public interface ISaleRepository
{
    /// <summary>The sale in progress, if any. Survives an app restart: the cashier picks up where it stopped.</summary>
    Task<Sale?> GetOpenAsync(CancellationToken cancellationToken = default);

    /// <summary>Saves the sale with its items and payments, replacing what was stored.</summary>
    Task SaveAsync(Sale sale, CancellationToken cancellationToken = default);
}
