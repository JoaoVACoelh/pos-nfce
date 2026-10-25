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
    }
}
