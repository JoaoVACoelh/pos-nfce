using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PosNfce.Checkout.Application.Contracts;
using PosNfce.Fiscal.Adapters.In;
using PosNfce.Fiscal.Adapters.Out.Catalog;
using PosNfce.Fiscal.Adapters.Out.Certificates;
using PosNfce.Fiscal.Adapters.Out.Files;
using PosNfce.Fiscal.Adapters.Out.Sefaz;
using PosNfce.Fiscal.Adapters.Out.Persistence;
using PosNfce.Fiscal.Adapters.Out.Security;
using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Issuers;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Application.Taxes;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Adapters;

public static class FiscalModule
{
    /// <summary>Registers the Fiscal module's use cases and adapters.</summary>
    /// <param name="dataFolder">Where the XML files are kept (xml/YYYY/MM inside it).</param>
    public static IServiceCollection AddFiscalModule(this IServiceCollection services, string connectionString, string dataFolder)
    {
        services.AddDbContext<FiscalDbContext>(options => options.UseSqlite(
            connectionString, sqlite => sqlite.MigrationsHistoryTable(FiscalDbContext.MigrationsHistoryTable)));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ICertificateReader, X509CertificateReader>();
        services.AddScoped<IIssuerRepository, IssuerRepository>();
        services.AddScoped<IFiscalDocumentRepository, FiscalDocumentRepository>();
        services.AddScoped<IDocumentNumbering, SqliteDocumentNumbering>();
        services.AddScoped<IIbsCbsRateRepository, IbsCbsRateRepository>();
        services.AddScoped<IApproximateTaxRates, CatalogApproximateTaxRates>();
        services.AddSingleton<IFiscalDocumentIssuer, UnimakeNfceIssuer>();
        services.AddSingleton<IXmlStore>(new FileXmlStore(dataFolder));
        services.AddScoped<IEventHandler<SaleCompleted>, SaleCompletedHandler>();
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<ISecretProtector, DpapiSecretProtector>();
        }

        services.AddScoped<GetIssuer>();
        services.AddScoped<SaveIssuer>();
        services.AddScoped<RegisterSaleDocument>();
        services.AddScoped<ListFiscalDocuments>();
        services.AddScoped<PrepareNfceXml>();
        services.AddScoped<IssueNfce>();
        services.AddScoped<RegisterIbsCbsRates>();
        services.AddScoped<ListIbsCbsRates>();

        return services;
    }

    /// <summary>Applies pending Fiscal migrations. Called once at startup.</summary>
    public static async Task MigrateFiscalDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<FiscalDbContext>().Database.MigrateAsync();
    }
}
