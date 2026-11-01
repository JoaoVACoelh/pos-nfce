using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosNfce.Fiscal.Adapters.Out.Persistence;
using PosNfce.Fiscal.Application.Taxes;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests.Application;

/// <summary>IBS/CBS rate periods, with fictitious values: the real ones come from the law and the accountant.</summary>
public sealed class IbsCbsRateTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public IbsCbsRateTests()
    {
        _connection.Open();
        using var db = NewContext();
        db.Database.Migrate();
    }

    public void Dispose() => _connection.Dispose();

    private FiscalDbContext NewContext() => new(new DbContextOptionsBuilder<FiscalDbContext>()
        .UseSqlite(_connection, sqlite => sqlite.MigrationsHistoryTable(FiscalDbContext.MigrationsHistoryTable))
        .Options);

    private Task Register(DateOnly from, decimal cbs) =>
        new RegisterIbsCbsRates(new IbsCbsRateRepository(NewContext())).ExecuteAsync(new(from, 0.1m, 0m, cbs, "TESTE"));

    [Fact]
    public async Task The_rates_in_force_are_the_latest_period_that_already_started()
    {
        await Register(new DateOnly(2026, 1, 1), 0.9m);
        await Register(new DateOnly(2027, 1, 1), 9m);
        var repository = new IbsCbsRateRepository(NewContext());

        Assert.Equal(0.9m, (await repository.FindForDateAsync(new DateOnly(2026, 12, 31)))!.Cbs);
        Assert.Equal(9m, (await repository.FindForDateAsync(new DateOnly(2027, 1, 1)))!.Cbs);
        Assert.Null(await repository.FindForDateAsync(new DateOnly(2025, 12, 31)));
    }

    [Fact]
    public async Task Two_periods_starting_on_the_same_day_are_not_allowed()
    {
        await Register(new DateOnly(2027, 1, 1), 9m);

        await Assert.ThrowsAsync<DomainException>(() => Register(new DateOnly(2027, 1, 1), 8m));
    }

    [Fact]
    public async Task List_shows_the_newest_period_first()
    {
        await Register(new DateOnly(2026, 1, 1), 0.9m);
        await Register(new DateOnly(2027, 1, 1), 9m);

        var periods = await new ListIbsCbsRates(new IbsCbsRateRepository(NewContext())).ExecuteAsync();

        Assert.Equal([new DateOnly(2027, 1, 1), new DateOnly(2026, 1, 1)], periods.Select(p => p.ValidFrom));
    }
}
