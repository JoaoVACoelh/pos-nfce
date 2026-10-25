using System.Runtime.Versioning;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosNfce.Checkout.Application.Contracts;
using PosNfce.Fiscal.Adapters.In;
using PosNfce.Fiscal.Adapters.Out.Certificates;
using PosNfce.Fiscal.Adapters.Out.Persistence;
using PosNfce.Fiscal.Adapters.Out.Security;
using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Issuers;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests.Application;

/// <summary>
/// From Checkout's event to a pending NFC-e, and number reservation. Uses a SQLite file (not in memory)
/// so the concurrency test gets separate connections, like two operations in the real app.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class FiscalDocumentUseCaseTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"posnfce-fiscal-{Guid.NewGuid():N}.db");

    public FiscalDocumentUseCaseTests()
    {
        using var db = NewContext();
        db.Database.Migrate();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }

    private FiscalDbContext NewContext() => new(new DbContextOptionsBuilder<FiscalDbContext>()
        .UseSqlite($"Data Source={_file}", sqlite => sqlite.MigrationsHistoryTable(FiscalDbContext.MigrationsHistoryTable))
        .Options);

    private SaleCompletedHandler NewHandler() => new FiscalTestKit(NewContext, Path.GetTempPath()).Handler();

    private Task<IReadOnlyList<FiscalDocumentSummary>> List() => new ListFiscalDocuments(new FiscalDocumentRepository(NewContext())).ExecuteAsync();

    private static SaleCompleted Sale(Guid? id = null) => new(
        id ?? Guid.NewGuid(), DateTimeOffset.Now, Subtotal: 15.98m, Discount: 0.98m, Total: 15m, Change: 5m,
        [
            new SoldItem(1, Guid.NewGuid(), "REF", "SEM GTIN", "Refrigerante", "UN", 2m, 5.49m, 10.98m,
                "22021000", null, 0, "5405", "500", "49", 0m, "49", 0m, "000", "000001", 0m),
            new SoldItem(2, Guid.NewGuid(), "PAO", "SEM GTIN", "Pão francês", "KG", 0.5m, 10m, 5m,
                "19059090", null, 0, "5102", "102", "49", 0m, "49", 0m, "000", "000001", 0m),
        ],
        [new ReceivedPayment("Cash", 20m, false)]);

    private async Task SaveIssuerAsync(long nextNumber) =>
        await new SaveIssuer(new IssuerRepository(NewContext(), new DpapiSecretProtector()), new X509CertificateReader(), TimeProvider.System)
            .ExecuteAsync(new SaveIssuerCommand("11222333000181", "0000000000000", "EMPRESA TESTE", null, "Rua A", "1", null,
                "Centro", "Belo Horizonte", "3106200", "MG", "30110000", null, 1, nextNumber, null, null, null, null));

    [Fact]
    public async Task Completed_sale_becomes_a_pending_document_with_the_discount_split()
    {
        await NewHandler().HandleAsync(Sale());

        var document = Assert.Single(await List());
        Assert.Equal((2, 15m, 0.98m, "—"), (document.ItemCount, document.Total, document.Discount, document.Number?.ToString() ?? "—"));

        await using var db = NewContext();
        var items = await db.Documents.Include(d => d.Items).SelectMany(d => d.Items).OrderBy(i => i.Line).ToListAsync();
        Assert.Equal(0.98m, items.Sum(i => i.Discount));
        Assert.Equal(["5405", "5102"], items.Select(i => i.Cfop));
    }

    [Fact]
    public async Task Same_sale_received_twice_creates_one_document()
    {
        var sale = Sale();

        await NewHandler().HandleAsync(sale);
        await NewHandler().HandleAsync(sale);

        Assert.Single(await List());
    }

    [Fact]
    public async Task Numbers_follow_the_issuer_sequence()
    {
        await SaveIssuerAsync(nextNumber: 41);
        await NewHandler().HandleAsync(Sale());
        await NewHandler().HandleAsync(Sale());
        var ids = (await List()).Select(d => d.Id).ToList();

        var first = await new SqliteDocumentNumbering(NewContext()).ReserveAsync(ids[0]);
        var second = await new SqliteDocumentNumbering(NewContext()).ReserveAsync(ids[1]);

        Assert.Equal([41L, 42L], new[] { first.Number, second.Number }.Order());
        await using var db = NewContext();
        Assert.Equal(43, (await db.Issuers.SingleAsync()).NextNumber);
    }

    [Fact]
    public async Task Concurrent_reservations_never_get_the_same_number()
    {
        await SaveIssuerAsync(nextNumber: 1);
        for (var i = 0; i < 10; i++)
        {
            await NewHandler().HandleAsync(Sale());
        }

        var ids = (await List()).Select(d => d.Id).ToList();
        var numbers = await Task.WhenAll(ids.Select(id => Task.Run(() => new SqliteDocumentNumbering(NewContext()).ReserveAsync(id))));

        Assert.Equal(Enumerable.Range(1, 10).Select(n => (long)n), numbers.Select(n => n.Number).Order());
    }

    [Fact]
    public async Task Reserving_without_an_issuer_gives_a_message()
    {
        await NewHandler().HandleAsync(Sale());
        var id = (await List()).Single().Id;

        var error = await Assert.ThrowsAsync<DomainException>(() => new SqliteDocumentNumbering(NewContext()).ReserveAsync(id));
        Assert.Contains("emitente", error.Message);
    }
}
