using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PosNfce.Fiscal.Adapters.Out.Certificates;
using PosNfce.Fiscal.Adapters.Out.Persistence;
using PosNfce.Fiscal.Adapters.Out.Security;
using PosNfce.Fiscal.Application.Issuers;
using PosNfce.Fiscal.Application.Ports;

namespace PosNfce.Fiscal.Adapters;

public static class FiscalModule
{
    /// <summary>Registers the Fiscal module's use cases and adapters.</summary>
    public static IServiceCollection AddFiscalModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FiscalDbContext>(options => options.UseSqlite(
            connectionString, sqlite => sqlite.MigrationsHistoryTable(FiscalDbContext.MigrationsHistoryTable)));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ICertificateReader, X509CertificateReader>();
        services.AddScoped<IIssuerRepository, IssuerRepository>();
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<ISecretProtector, DpapiSecretProtector>();
        }

        services.AddScoped<GetIssuer>();
        services.AddScoped<SaveIssuer>();

        return services;
    }

    /// <summary>Applies pending Fiscal migrations. Called once at startup.</summary>
    public static async Task MigrateFiscalDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<FiscalDbContext>().Database.MigrateAsync();
    }
}
