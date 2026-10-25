using Microsoft.EntityFrameworkCore;
using PosNfce.Catalog.Application.Ports;
using PosNfce.Catalog.Domain.Products;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Adapters.Out.Persistence;

public sealed class ProductRepository(CatalogDbContext db) : IProductRepository
{
    public async Task<Product?> FindByGtinAsync(Gtin gtin, CancellationToken cancellationToken = default)
    {
        var record = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Gtin == gtin.Value, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<Product?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var record = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Code == code, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<Product?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string? term, int limit, CancellationToken cancellationToken = default)
    {
        var query = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(term))
        {
            // LIKE in SQLite ignores case for ASCII letters; % and _ typed by the user are escaped so they match literally.
            var pattern = $"%{term.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_")}%";
            query = query.Where(p =>
                EF.Functions.Like(p.Description, pattern, @"\")
                || EF.Functions.Like(p.Code, pattern, @"\")
                || EF.Functions.Like(p.Gtin, pattern, @"\"));
        }

        var records = await query.OrderBy(p => p.Description).Take(limit).ToListAsync(cancellationToken);
        return records.Select(ToDomain).ToList();
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        db.Products.Add(ToRecord(product));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        db.Products.Update(ToRecord(product));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static Product ToDomain(ProductRecord r) => Product.Restore(
        r.Id, r.Code, Gtin.Parse(r.Gtin), r.Description, Ncm.Parse(r.Ncm), Cest.ParseOptional(r.Cest),
        r.Unit, new Money(r.Price), (ProductOrigin)r.Origin, r.TaxRuleId, r.Active);

    private static ProductRecord ToRecord(Product p) => new()
    {
        Id = p.Id,
        Code = p.Code,
        Gtin = p.Gtin.Value,
        Description = p.Description,
        Ncm = p.Ncm.Value,
        Cest = p.Cest?.Value,
        Unit = p.Unit,
        Price = p.Price.Amount,
        Origin = (int)p.Origin,
        TaxRuleId = p.TaxRuleId,
        Active = p.Active,
    };
}
