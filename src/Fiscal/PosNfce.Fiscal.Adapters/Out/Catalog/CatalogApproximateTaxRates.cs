using PosNfce.Catalog.Application.Ibpt;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Taxes;

namespace PosNfce.Fiscal.Adapters.Out.Catalog;

/// <summary>Implements Fiscal's port with Catalog's IBPT use case: Fiscal never reads Catalog's tables.</summary>
public sealed class CatalogApproximateTaxRates(FindIbptRate findRate) : IApproximateTaxRates
{
    public async Task<ApproximateRatesWithSource?> FindAsync(string ncm, int origin, CancellationToken cancellationToken = default)
    {
        var rate = await findRate.ExecuteAsync(ncm, ex: null, origin, cancellationToken);
        return rate is null
            ? null
            : new ApproximateRatesWithSource(new ApproximateRates(rate.Federal, rate.State, rate.Municipal), $"{rate.Source} {rate.Version}".Trim());
    }
}
