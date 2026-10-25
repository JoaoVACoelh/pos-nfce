using PosNfce.Catalog.Application.Ports;
using PosNfce.Catalog.Domain.Products;

namespace PosNfce.Catalog.Application.Ibpt;

/// <summary>Approximate tax rates for a product, already resolved for its origin. Fiscal uses it in Stage 3 for the receipt.</summary>
public sealed record ApproximateTaxRates(decimal Federal, decimal State, decimal Municipal, string Source, string Version);

public sealed class FindIbptRate(IIbptRateRepository rates)
{
    public async Task<ApproximateTaxRates?> ExecuteAsync(
        string ncm, string? ex, ProductOrigin origin, CancellationToken cancellationToken = default)
    {
        var rate = await rates.FindAsync(Ncm.Parse(ncm), ex?.Trim() ?? "", cancellationToken);

        return rate is null
            ? null
            : new ApproximateTaxRates(rate.FederalFor(origin).Value, rate.State.Value, rate.Municipal.Value, rate.Source, rate.Version);
    }
}
