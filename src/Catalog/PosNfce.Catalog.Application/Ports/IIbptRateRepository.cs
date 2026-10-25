using PosNfce.Catalog.Domain.Ibpt;
using PosNfce.Catalog.Domain.Products;

namespace PosNfce.Catalog.Application.Ports;

public interface IIbptRateRepository
{
    /// <summary>Replaces the whole table in one transaction: a failed import leaves the previous table intact.</summary>
    Task ReplaceAllAsync(IReadOnlyList<IbptRate> rates, CancellationToken cancellationToken = default);

    Task<IbptRate?> FindAsync(Ncm ncm, string ex, CancellationToken cancellationToken = default);

    /// <summary>Version, validity and row count of the table in use; null when nothing was imported.</summary>
    Task<IbptTableSummary?> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public sealed record IbptTableSummary(string Version, DateOnly ValidFrom, DateOnly ValidUntil, int Count);
