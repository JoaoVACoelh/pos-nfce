using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PosNfce.Catalog.Adapters.Out.Persistence;

/// <summary>Used only by <c>dotnet ef</c> to create migrations; the app builds its own context through DI.</summary>
public sealed class CatalogDesignTimeDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
