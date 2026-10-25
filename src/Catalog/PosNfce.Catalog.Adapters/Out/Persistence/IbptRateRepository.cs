using Microsoft.EntityFrameworkCore;
using PosNfce.Catalog.Application.Ports;
using PosNfce.Catalog.Domain.Ibpt;
using PosNfce.Catalog.Domain.Products;
using PosNfce.Catalog.Domain.TaxRules;

namespace PosNfce.Catalog.Adapters.Out.Persistence;

public sealed class IbptRateRepository(CatalogDbContext db) : IIbptRateRepository
{
    public async Task ReplaceAllAsync(IReadOnlyList<IbptRate> rates, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.IbptRates.ExecuteDeleteAsync(cancellationToken);
        db.IbptRates.AddRange(rates.Select(ToRecord));
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    public async Task<IbptRate?> FindAsync(Ncm ncm, string ex, CancellationToken cancellationToken = default)
    {
        var record = await db.IbptRates.AsNoTracking()
            .SingleOrDefaultAsync(r => r.Ncm == ncm.Value && r.Ex == ex, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IbptTableSummary?> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var count = await db.IbptRates.CountAsync(cancellationToken);
        if (count == 0)
        {
            return null;
        }

        var version = await db.IbptRates.Select(r => r.Version).FirstAsync(cancellationToken);
        var validFrom = await db.IbptRates.MinAsync(r => r.ValidFrom, cancellationToken);
        var validUntil = await db.IbptRates.MaxAsync(r => r.ValidUntil, cancellationToken);
        return new IbptTableSummary(version, validFrom, validUntil, count);
    }

    private static IbptRate ToDomain(IbptRateRecord r) => new(
        Ncm.Parse(r.Ncm), r.Ex, r.Description,
        Percentage.Of(r.FederalNational, "federal"), Percentage.Of(r.FederalImported, "federal importados"),
        Percentage.Of(r.State, "estadual"), Percentage.Of(r.Municipal, "municipal"),
        r.ValidFrom, r.ValidUntil, r.Version, r.Source);

    private static IbptRateRecord ToRecord(IbptRate r) => new()
    {
        Ncm = r.Ncm.Value,
        Ex = r.Ex,
        Description = r.Description,
        FederalNational = r.FederalNational.Value,
        FederalImported = r.FederalImported.Value,
        State = r.State.Value,
        Municipal = r.Municipal.Value,
        ValidFrom = r.ValidFrom,
        ValidUntil = r.ValidUntil,
        Version = r.Version,
        Source = r.Source,
    };
}
