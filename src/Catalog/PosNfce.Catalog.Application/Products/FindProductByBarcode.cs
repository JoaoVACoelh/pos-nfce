using PosNfce.Catalog.Application.Ports;
using PosNfce.Catalog.Domain.Products;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Application.Products;

/// <summary>What the checkout needs to know about a scanned product.</summary>
public sealed record ProductLookup(Guid Id, string Code, string Gtin, string Description, string Unit, Money Price);

/// <summary>
/// Finds an active product by what the scanner or the cashier typed: a GTIN or, failing that, the internal code.
/// </summary>
public sealed class FindProductByBarcode(IProductRepository products)
{
    public async Task<ProductLookup?> ExecuteAsync(string typed, CancellationToken cancellationToken = default)
    {
        var product = await FindActiveAsync(products, typed, cancellationToken);

        return product is null
            ? null
            : new ProductLookup(product.Id, product.Code, product.Gtin.Value, product.Description, product.Unit, product.Price);
    }

    internal static async Task<Product?> FindActiveAsync(IProductRepository products, string typed, CancellationToken cancellationToken)
    {
        var input = typed.Trim();
        if (input.Length == 0)
        {
            return null;
        }

        var product = await FindByGtinAsync(products, input, cancellationToken)
            ?? await products.FindByCodeAsync(input, cancellationToken);

        return product is { Active: true } ? product : null;
    }

    private static async Task<Product?> FindByGtinAsync(IProductRepository products, string input, CancellationToken cancellationToken)
    {
        try
        {
            var gtin = Gtin.Parse(input);
            return gtin.HasBarcode ? await products.FindByGtinAsync(gtin, cancellationToken) : null;
        }
        catch (DomainException)
        {
            // Not a valid GTIN: it may still be an internal code.
            return null;
        }
    }
}
