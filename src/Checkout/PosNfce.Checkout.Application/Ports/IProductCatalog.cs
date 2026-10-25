using PosNfce.Checkout.Domain.Sales;

namespace PosNfce.Checkout.Application.Ports;

/// <summary>What Checkout needs from the product catalog. The adapter calls the Catalog module's use case.</summary>
public interface IProductCatalog
{
    /// <summary>Active product by GTIN or internal code; null when not found.</summary>
    Task<ProductToSell?> FindAsync(string code, CancellationToken cancellationToken = default);
}

public sealed record ProductToSell(
    Guid Id, string Code, string Gtin, string Description, string Unit, decimal Price, TaxSnapshot Tax);
