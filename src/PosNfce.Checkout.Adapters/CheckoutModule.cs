using Microsoft.Extensions.DependencyInjection;

namespace PosNfce.Checkout.Adapters;

public static class CheckoutModule
{
    /// <summary>Registers the Checkout module's use cases and adapters.</summary>
    public static IServiceCollection AddCheckoutModule(this IServiceCollection services)
    {
        return services;
    }
}
