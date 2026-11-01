using Microsoft.EntityFrameworkCore;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Taxes;

namespace PosNfce.Fiscal.Adapters.Out.Persistence;

/// <summary>Row of <c>fiscal_ibs_cbs_rates</c>, one per period.</summary>
public sealed class IbsCbsRateRecord
{
    public DateOnly ValidFrom { get; set; }
    public decimal IbsState { get; set; }
    public decimal IbsMunicipal { get; set; }
    public decimal Cbs { get; set; }
    public string Source { get; set; } = "";
}

public sealed class IbsCbsRateRepository(FiscalDbContext db) : IIbsCbsRateRepository
{
    public async Task<IbsCbsRates?> FindForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var record = await db.IbsCbsRates.AsNoTracking()
            .Where(r => r.ValidFrom <= date)
            .OrderByDescending(r => r.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public Task<bool> ExistsFromAsync(DateOnly validFrom, CancellationToken cancellationToken = default) =>
        db.IbsCbsRates.AnyAsync(r => r.ValidFrom == validFrom, cancellationToken);

    public async Task<IReadOnlyList<IbsCbsRates>> ListAsync(CancellationToken cancellationToken = default) =>
        (await db.IbsCbsRates.AsNoTracking().ToListAsync(cancellationToken)).Select(ToDomain).ToList();

    public async Task AddAsync(IbsCbsRates rates, CancellationToken cancellationToken = default)
    {
        db.IbsCbsRates.Add(new IbsCbsRateRecord
        {
            ValidFrom = rates.ValidFrom,
            IbsState = rates.IbsState,
            IbsMunicipal = rates.IbsMunicipal,
            Cbs = rates.Cbs,
            Source = rates.Source,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static IbsCbsRates ToDomain(IbsCbsRateRecord r) => new(r.ValidFrom, r.IbsState, r.IbsMunicipal, r.Cbs, r.Source);
}
