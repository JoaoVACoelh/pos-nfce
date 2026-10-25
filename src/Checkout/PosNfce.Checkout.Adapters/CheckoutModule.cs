using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PosNfce.Checkout.Adapters.Out.Catalog;
using PosNfce.Checkout.Adapters.Out.Payments;
using PosNfce.Checkout.Adapters.Out.Persistence;
using PosNfce.Checkout.Application.Ports;
using PosNfce.Checkout.Application.Sales;

namespace PosNfce.Checkout.Adapters;

public static class CheckoutModule
{
    /// <summary>Registers the Checkout module's use cases and adapters. Needs the Catalog module registered too.</summary>
    public static IServiceCollection AddCheckoutModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CheckoutDbContext>(options => options.UseSqlite(
            connectionString, sqlite => sqlite.MigrationsHistoryTable(CheckoutDbContext.MigrationsHistoryTable)));

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IProductCatalog, CatalogProductLookup>();
        services.AddSingleton<ICardPaymentGateway, StandaloneTerminalGateway>();

        services.AddScoped<GetCurrentSale>();
        services.AddScoped<AddItemToSale>();
        services.AddScoped<RemoveItemFromSale>();
        services.AddScoped<ApplySaleDiscount>();
        services.AddScoped<AddSalePayment>();
        services.AddScoped<RemoveSalePayment>();
        services.AddScoped<CompleteSale>();
        services.AddScoped<CancelSale>();
        services.AddScoped<IdentifySaleCustomer>();

        return services;
    }

    /// <summary>Applies pending Checkout migrations. Called once at startup.</summary>
    public static async Task MigrateCheckoutDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CheckoutDbContext>().Database.MigrateAsync();
    }
}
