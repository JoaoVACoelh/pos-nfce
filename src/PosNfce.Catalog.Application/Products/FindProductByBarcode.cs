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
        var input = typed.Trim();
        if (input.Length == 0)
        {
            return null;
        }

        var product = await FindByGtinAsync(input, cancellationToken)
            ?? await products.FindByCodeAsync(input, cancellationToken);

        return product is { Active: true }
            ? new ProductLookup(product.Id, product.Code, product.Gtin.Value, product.Description, product.Unit, product.Price)
            : null;
    }

    private async Task<Product?> FindByGtinAsync(string input, CancellationToken cancellationToken)
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
