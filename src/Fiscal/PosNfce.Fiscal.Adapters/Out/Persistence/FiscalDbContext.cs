using Microsoft.EntityFrameworkCore;

namespace PosNfce.Fiscal.Adapters.Out.Persistence;

public sealed class FiscalDbContext(DbContextOptions<FiscalDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Each module keeps its own migration history in the shared database file,
    /// so one module's migrations never look like pending changes of another.
    /// </summary>
    public const string MigrationsHistoryTable = "__fiscal_migrations";

    public DbSet<IssuerRecord> Issuers => Set<IssuerRecord>();

    public DbSet<FiscalDocumentRecord> Documents => Set<FiscalDocumentRecord>();

    public DbSet<IbsCbsRateRecord> IbsCbsRates => Set<IbsCbsRateRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IssuerRecord>(issuer =>
        {
            issuer.ToTable("fiscal_issuers");
            issuer.HasKey(i => i.Id);
            issuer.Property(i => i.Cnpj).HasMaxLength(14);
            issuer.Property(i => i.StateRegistration).HasMaxLength(14);
            issuer.Property(i => i.LegalName).HasMaxLength(60);
            issuer.Property(i => i.TradeName).HasMaxLength(60);
            issuer.Property(i => i.Street).HasMaxLength(60);
            issuer.Property(i => i.Number).HasMaxLength(60);
            issuer.Property(i => i.Complement).HasMaxLength(60);
            issuer.Property(i => i.District).HasMaxLength(60);
            issuer.Property(i => i.CityName).HasMaxLength(60);
            issuer.Property(i => i.CityCode).HasMaxLength(7);
            issuer.Property(i => i.State).HasMaxLength(2);
            issuer.Property(i => i.PostalCode).HasMaxLength(8);
            issuer.Property(i => i.Phone).HasMaxLength(14);
            issuer.Property(i => i.CscTokenId).HasMaxLength(6);
            issuer.Property(i => i.CertificateCnpj).HasMaxLength(14);
        });

        modelBuilder.Entity<FiscalDocumentRecord>(document =>
        {
            document.ToTable("fiscal_documents");
            document.HasKey(d => d.Id);
            document.HasIndex(d => d.SaleId).IsUnique();
            document.Property(d => d.AccessKey).HasMaxLength(44);
            document.HasIndex(d => d.Status);
            // Two documents can never share a number; NULL (not numbered yet) is allowed many times.
            document.HasIndex(d => new { d.Series, d.Number }).IsUnique();
            document.HasMany(d => d.Items).WithOne().HasForeignKey(i => i.DocumentId).OnDelete(DeleteBehavior.Cascade);
            document.HasMany(d => d.Payments).WithOne().HasForeignKey(p => p.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FiscalItemRecord>(item =>
        {
            item.ToTable("fiscal_document_items");
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

        modelBuilder.Entity<IbsCbsRateRecord>(rate =>
        {
            rate.ToTable("fiscal_ibs_cbs_rates");
            rate.HasKey(r => r.ValidFrom);
            rate.Property(r => r.Source).HasMaxLength(200);
        });

        modelBuilder.Entity<FiscalPaymentRecord>(payment =>
        {
            payment.ToTable("fiscal_document_payments");
            payment.Property(p => p.Method).HasMaxLength(20);
        });
    }
}
