using Microsoft.EntityFrameworkCore;
using PosNfce.Catalog.Domain.Products;

namespace PosNfce.Catalog.Adapters.Out.Persistence;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<ProductRecord> Products => Set<ProductRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductRecord>(product =>
        {
            product.ToTable("catalog_products");
            product.HasKey(p => p.Id);
            product.Property(p => p.Code).HasMaxLength(Product.CodeMaxLength);
            product.Property(p => p.Gtin).HasMaxLength(14);
            product.Property(p => p.Description).HasMaxLength(Product.DescriptionMaxLength);
            product.Property(p => p.Ncm).HasMaxLength(8);
            product.Property(p => p.Cest).HasMaxLength(7);
            product.Property(p => p.Unit).HasMaxLength(Product.UnitMaxLength);

            product.HasIndex(p => p.Code).IsUnique();
            // Many products have no barcode ("SEM GTIN"), so uniqueness only applies to real GTINs.
            product.HasIndex(p => p.Gtin).IsUnique().HasFilter($"\"Gtin\" <> '{Gtin.NoGtinText}'");
        });
    }
}
