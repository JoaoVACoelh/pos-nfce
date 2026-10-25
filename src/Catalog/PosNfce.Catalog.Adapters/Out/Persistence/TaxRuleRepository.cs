using Microsoft.EntityFrameworkCore;
using PosNfce.Catalog.Application.Ports;
using PosNfce.Catalog.Domain.TaxRules;

namespace PosNfce.Catalog.Adapters.Out.Persistence;

public sealed class TaxRuleRepository(CatalogDbContext db) : ITaxRuleRepository
{
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.TaxRules.AnyAsync(r => r.Id == id, cancellationToken);

    public async Task<TaxRule?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await db.TaxRules.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        // SQLite's lower() only folds ASCII letters: "Água" and "ÁGUA" still count as different names.
        var lowered = name.ToLower();
        return db.TaxRules.AnyAsync(r => r.Name.ToLower() == lowered, cancellationToken);
    }

    public async Task<IReadOnlyList<TaxRule>> ListAsync(CancellationToken cancellationToken = default)
    {
        var records = await db.TaxRules.AsNoTracking().ToListAsync(cancellationToken);
        return records.Select(ToDomain).ToList();
    }

    public async Task AddAsync(TaxRule taxRule, CancellationToken cancellationToken = default)
    {
        db.TaxRules.Add(ToRecord(taxRule));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static TaxRule ToDomain(TaxRuleRecord r) => TaxRule.Restore(
        r.Id,
        r.Name,
        Cfop.Parse(r.Cfop),
        Csosn.Parse(r.Csosn),
        PisCofinsCst.Parse(r.PisCst, "PIS"),
        Percentage.Of(r.PisRate, "PIS"),
        PisCofinsCst.Parse(r.CofinsCst, "COFINS"),
        Percentage.Of(r.CofinsRate, "COFINS"),
        TaxReformCodes.Parse(r.IbsCbsCst, r.ClassTrib));

    private static TaxRuleRecord ToRecord(TaxRule t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Cfop = t.Cfop.Value,
        Csosn = t.Csosn.Value,
        PisCst = t.PisCst.Value,
        PisRate = t.PisRate.Value,
        CofinsCst = t.CofinsCst.Value,
        CofinsRate = t.CofinsRate.Value,
        IbsCbsCst = t.TaxReform.IbsCbsCst,
        ClassTrib = t.TaxReform.ClassTrib,
    };
}
