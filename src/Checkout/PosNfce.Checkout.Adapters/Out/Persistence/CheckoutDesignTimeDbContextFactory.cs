using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PosNfce.Checkout.Adapters.Out.Persistence;

/// <summary>Used only by <c>dotnet ef</c> to create migrations; the app builds its own context through DI.</summary>
public sealed class CheckoutDesignTimeDbContextFactory : IDesignTimeDbContextFactory<CheckoutDbContext>
{
    public CheckoutDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<CheckoutDbContext>()
            .UseSqlite("Data Source=design-time.db", sqlite => sqlite.MigrationsHistoryTable(CheckoutDbContext.MigrationsHistoryTable))
            .Options);
}
