using Microsoft.EntityFrameworkCore;
using PosNfce.Catalog.Domain.Products;
using PosNfce.Catalog.Domain.TaxRules;

namespace PosNfce.Catalog.Adapters.Out.Persistence;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<ProductRecord> Products => Set<ProductRecord>();

    public DbSet<TaxRuleRecord> TaxRules => Set<TaxRuleRecord>();

    public DbSet<IbptRateRecord> IbptRates => Set<IbptRateRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IbptRateRecord>(rate =>
        {
            rate.ToTable("catalog_ibpt_rates");
            rate.HasKey(r => r.Id);
            rate.Property(r => r.Ncm).HasMaxLength(8);
            rate.Property(r => r.Ex).HasMaxLength(3);
            rate.Property(r => r.Version).HasMaxLength(20);
            rate.Property(r => r.Source).HasMaxLength(100);
            rate.HasIndex(r => new { r.Ncm, r.Ex }).IsUnique();
        });

        modelBuilder.Entity<TaxRuleRecord>(taxRule =>
        {
            taxRule.ToTable("catalog_tax_rules");
            taxRule.HasKey(t => t.Id);
            taxRule.Property(t => t.Name).HasMaxLength(TaxRule.NameMaxLength);
            taxRule.Property(t => t.Cfop).HasMaxLength(4);
            taxRule.Property(t => t.Csosn).HasMaxLength(3);
            taxRule.Property(t => t.PisCst).HasMaxLength(2);
            taxRule.Property(t => t.CofinsCst).HasMaxLength(2);
            taxRule.Property(t => t.IbsCbsCst).HasMaxLength(3);
            taxRule.Property(t => t.ClassTrib).HasMaxLength(6);

            taxRule.HasIndex(t => t.Name).IsUnique();
        });

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

            // A rule in use can't be deleted: products would be left without tax data.
            product.HasOne<TaxRuleRecord>().WithMany().HasForeignKey(p => p.TaxRuleId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
