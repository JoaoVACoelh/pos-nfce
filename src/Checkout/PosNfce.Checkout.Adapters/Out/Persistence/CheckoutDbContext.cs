using Microsoft.EntityFrameworkCore;

namespace PosNfce.Checkout.Adapters.Out.Persistence;

public sealed class CheckoutDbContext(DbContextOptions<CheckoutDbContext> options) : DbContext(options)
{
    /// <summary>Own migration history in the shared database file, like the Fiscal module.</summary>
    public const string MigrationsHistoryTable = "__checkout_migrations";

    public DbSet<SaleRecord> Sales => Set<SaleRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SaleRecord>(sale =>
        {
            sale.ToTable("checkout_sales");
            sale.HasKey(s => s.Id);
            sale.HasIndex(s => s.Status);
            sale.HasMany(s => s.Items).WithOne().HasForeignKey(i => i.SaleId).OnDelete(DeleteBehavior.Cascade);
            sale.HasMany(s => s.Payments).WithOne().HasForeignKey(p => p.SaleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SaleItemRecord>(item =>
        {
            item.ToTable("checkout_sale_items");
            item.HasKey(i => i.Id);
            item.Property(i => i.Code).HasMaxLength(60);
            item.Property(i => i.Gtin).HasMaxLength(14);
            item.Property(i => i.Description).HasMaxLength(120);
            item.Property(i => i.Unit).HasMaxLength(6);
            item.Property(i => i.Ncm).HasMaxLength(8);
            item.Property(i => i.Cest).HasMaxLength(7);
            item.Property(i => i.Cfop).HasMaxLength(4);
            item.Property(i => i.Csosn).HasMaxLength(3);
            item.Property(i => i.PisCst).HasMaxLength(2);
            item.Property(i => i.CofinsCst).HasMaxLength(2);
            item.Property(i => i.IbsCbsCst).HasMaxLength(3);
            item.Property(i => i.ClassTrib).HasMaxLength(6);
        });

        modelBuilder.Entity<PaymentRecord>(payment =>
        {
            payment.ToTable("checkout_payments");
            payment.HasKey(p => p.Id);
        });
    }
}
