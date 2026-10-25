using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PosNfce.Catalog.Adapters.Out.Ibpt;
using PosNfce.Catalog.Adapters.Out.Persistence;
using PosNfce.Catalog.Application.Ibpt;
using PosNfce.Catalog.Application.Ports;
using PosNfce.Catalog.Application.Products;
using PosNfce.Catalog.Application.TaxRules;

namespace PosNfce.Catalog.Adapters;

public static class CatalogModule
{
    /// <summary>Registers the Catalog module's use cases and adapters.</summary>
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CatalogDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ITaxRuleRepository, TaxRuleRepository>();
        services.AddScoped<IIbptRateRepository, IbptRateRepository>();
        services.AddSingleton<IIbptFileReader, IbptCsvReader>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<RegisterProduct>();
        services.AddScoped<FindProductByBarcode>();
        services.AddScoped<FindProductForSale>();
        services.AddScoped<ListProducts>();
        services.AddScoped<UpdateProduct>();
        services.AddScoped<SetProductActive>();
        services.AddScoped<RegisterTaxRule>();
        services.AddScoped<ListTaxRules>();
        services.AddScoped<ImportIbptTable>();
        services.AddScoped<GetIbptStatus>();
        services.AddScoped<FindIbptRate>();

        return services;
    }

    /// <summary>Applies pending Catalog migrations. Called once at startup.</summary>
    public static async Task MigrateCatalogDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    }
}
