using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PosNfce.Fiscal.Adapters.Out.Persistence;

/// <summary>Used only by <c>dotnet ef</c> to create migrations; the app builds its own context through DI.</summary>
public sealed class FiscalDesignTimeDbContextFactory : IDesignTimeDbContextFactory<FiscalDbContext>
{
    public FiscalDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<FiscalDbContext>()
            .UseSqlite("Data Source=design-time.db", sqlite => sqlite.MigrationsHistoryTable(FiscalDbContext.MigrationsHistoryTable))
            .Options);
}
