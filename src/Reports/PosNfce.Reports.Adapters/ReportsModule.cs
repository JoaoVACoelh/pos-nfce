using Microsoft.Extensions.DependencyInjection;

namespace PosNfce.Reports.Adapters;

public static class ReportsModule
{
    /// <summary>Registers the Reports module's use cases and adapters.</summary>
    public static IServiceCollection AddReportsModule(this IServiceCollection services)
    {
        return services;
    }
}
