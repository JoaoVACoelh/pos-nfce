using Microsoft.Extensions.DependencyInjection;

namespace PosNfce.Catalog.Adapters;

public static class CatalogModule
{
    /// <summary>Registers the Catalog module's use cases and adapters.</summary>
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        return services;
    }
}
