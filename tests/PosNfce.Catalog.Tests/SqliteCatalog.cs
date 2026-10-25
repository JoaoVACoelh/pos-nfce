using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosNfce.Catalog.Adapters.Out.Persistence;

namespace PosNfce.Catalog.Tests;

/// <summary>
/// Real SQLite database in memory, built from the migrations. It lives while the connection is open,
/// so each test class gets a fresh one.
/// </summary>
public sealed class SqliteCatalog : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public SqliteCatalog()
    {
        _connection.Open();
        using var db = NewContext();
        db.Database.Migrate();
    }

    public CatalogDbContext NewContext() =>
        new(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
