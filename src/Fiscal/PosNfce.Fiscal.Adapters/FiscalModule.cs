using Microsoft.Extensions.DependencyInjection;

namespace PosNfce.Fiscal.Adapters;

public static class FiscalModule
{
    /// <summary>Registers the Fiscal module's use cases and adapters.</summary>
    public static IServiceCollection AddFiscalModule(this IServiceCollection services)
    {
        return services;
    }
}
