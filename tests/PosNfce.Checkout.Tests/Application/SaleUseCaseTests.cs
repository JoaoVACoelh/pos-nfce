using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosNfce.Checkout.Adapters.Out.Payments;
using PosNfce.Checkout.Adapters.Out.Persistence;
using PosNfce.Checkout.Application.Contracts;
using PosNfce.Checkout.Application.Ports;
using PosNfce.Checkout.Application.Sales;
using PosNfce.Checkout.Domain.Sales;
using PosNfce.Checkout.Tests.Domain;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Tests.Application;

/// <summary>Use cases with the real repository on SQLite in memory; catalog and event publisher are fakes.</summary>
public sealed class SaleUseCaseTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly FakeCatalog _catalog = new();
    private readonly RecordingPublisher _events = new();

    public SaleUseCaseTests()
    {
        _connection.Open();
        using var db = NewContext();
        db.Database.Migrate();

        _catalog.Add(new ProductToSell(Guid.NewGuid(), "REF", "7891234567895", "Refrigerante", "UN", 5.49m, SaleTests.Tax));
        _catalog.Add(new ProductToSell(Guid.NewGuid(), "PAO", "SEM GTIN", "Pão francês", "KG", 12.90m, SaleTests.Tax));
    }

    public void Dispose() => _connection.Dispose();

    private CheckoutDbContext NewContext() => new(new DbContextOptionsBuilder<CheckoutDbContext>()
        .UseSqlite(_connection, sqlite => sqlite.MigrationsHistoryTable(CheckoutDbContext.MigrationsHistoryTable))
        .Options);

    private SaleRepository Repository() => new(NewContext());

    private Task<SaleView> Scan(string typed) => new AddItemToSale(Repository(), _catalog, TimeProvider.System).ExecuteAsync(typed);

    private Task<SaleView> Pay(PaymentMethod method, decimal amount) => new AddSalePayment(Repository(), new StandaloneTerminalGateway()).ExecuteAsync(method, amount);

    [Fact]
    public async Task Full_sale_is_saved_and_publishes_sale_completed()
    {
        await Scan("2*7891234567895");
        await Scan("0,5*PAO");
        await new ApplySaleDiscount(Repository()).ExecuteAsync(0.43m);
        var view = await Pay(PaymentMethod.Cash, 20m);
        Assert.Equal(17.00m, view.Total); // 10,98 + 6,45 - 0,43

        var completed = await new CompleteSale(Repository(), _events, TimeProvider.System).ExecuteAsync();

        Assert.Equal(3.00m, completed.Change);
        var published = Assert.Single(_events.Published.OfType<SaleCompleted>());
        Assert.Equal((17.00m, 0.43m, 3.00m), (published.Total, published.Discount, published.Change));
        Assert.Equal(["REF", "PAO"], published.Items.Select(i => i.Code));
        Assert.Equal("5102", published.Items[0].Cfop);
        Assert.Equal("Cash", Assert.Single(published.Payments).Method);
        Assert.Equal(SaleView.Empty, await new GetCurrentSale(Repository()).ExecuteAsync());
    }

    [Fact]
    public async Task Open_sale_survives_a_restart()
    {
        await Scan("7891234567895");

        // A new repository and context, as after closing and reopening the app.
        var view = await new GetCurrentSale(Repository()).ExecuteAsync();

        Assert.Equal("Refrigerante", Assert.Single(view.Items).Description);
    }

    [Fact]
    public async Task Unknown_product_gives_a_message_and_no_sale()
    {
        var error = await Assert.ThrowsAsync<DomainException>(() => Scan("123"));

        Assert.Contains("não encontrado", error.Message);
        Assert.Null(await Repository().GetOpenAsync());
    }

    [Fact]
    public async Task Removed_item_and_payment_are_gone_after_reload()
    {
        var view = await Scan("7891234567895");
        await Scan("PAO");
        view = await new RemoveItemFromSale(Repository()).ExecuteAsync(view.Items[0].Id);
        view = await Pay(PaymentMethod.Pix, 5m);
        view = await new RemoveSalePayment(Repository()).ExecuteAsync(view.Payments[0].Id);

        var reloaded = await new GetCurrentSale(Repository()).ExecuteAsync();
        Assert.Equal(["PAO"], reloaded.Items.Select(i => i.Code));
        Assert.Empty(reloaded.Payments);
    }

    [Fact]
    public async Task Cancelled_sale_is_closed_and_publishes_nothing()
    {
        await Scan("7891234567895");

        await new CancelSale(Repository(), TimeProvider.System).ExecuteAsync();

        Assert.Equal(SaleView.Empty, await new GetCurrentSale(Repository()).ExecuteAsync());
        Assert.Empty(_events.Published);
    }

    [Fact]
    public async Task Quantity_keeps_four_decimals_after_a_round_trip()
    {
        await Scan("0,3755*PAO");

        var item = Assert.Single((await new GetCurrentSale(Repository()).ExecuteAsync()).Items);
        Assert.Equal((0.3755m, 4.84m), (item.Quantity, item.Total));
    }

    [Fact]
    public async Task Card_payment_the_sale_refuses_is_never_charged()
    {
        await Scan("7891234567895"); // R$ 5,49
        var gateway = new CountingGateway();

        await Assert.ThrowsAsync<DomainException>(
            () => new AddSalePayment(Repository(), gateway).ExecuteAsync(PaymentMethod.CreditCard, 10m));

        Assert.Equal(0, gateway.Charges);
    }

    [Fact]
    public async Task Cash_is_not_sent_to_the_card_gateway()
    {
        await Scan("7891234567895");
        var gateway = new CountingGateway();

        await new AddSalePayment(Repository(), gateway).ExecuteAsync(PaymentMethod.Cash, 10m);

        Assert.Equal(0, gateway.Charges);
    }

    [Fact]
    public async Task Failing_reaction_keeps_the_sale_and_returns_a_warning()
    {
        await Scan("7891234567895");
        await Pay(PaymentMethod.Cash, 10m);

        var completed = await new CompleteSale(Repository(), new FailingPublisher(), TimeProvider.System).ExecuteAsync();

        Assert.Contains("nota fiscal", completed.AfterSaleWarning);
        Assert.Equal(SaleView.Empty, await new GetCurrentSale(Repository()).ExecuteAsync()); // sale closed anyway
    }

    private sealed class FailingPublisher : IEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent => throw new InvalidOperationException("banco indisponível");
    }

    private sealed class CountingGateway : ICardPaymentGateway
    {
        public int Charges { get; private set; }

        public Task<CardCharge> ChargeAsync(PaymentMethod method, Money amount, CancellationToken cancellationToken = default)
        {
            Charges++;
            return Task.FromResult(new CardCharge(Integrated: true));
        }
    }

    private sealed class FakeCatalog : IProductCatalog
    {
        private readonly List<ProductToSell> _products = [];

        public void Add(ProductToSell product) => _products.Add(product);

        public Task<ProductToSell?> FindAsync(string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(_products.FirstOrDefault(p => p.Gtin == code || p.Code == code));
    }

    private sealed class RecordingPublisher : IEventPublisher
    {
        public List<object> Published { get; } = [];

        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            Published.Add(@event);
            return Task.CompletedTask;
        }
    }
}
