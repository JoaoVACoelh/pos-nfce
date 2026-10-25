using PosNfce.Catalog.Application.Ports;

namespace PosNfce.Catalog.Application.Products;

/// <summary>
/// Everything the checkout copies into a sale item: price and the full tax data (product + its tax rule).
/// Plain types only, so other modules don't depend on Catalog's domain.
/// </summary>
public sealed record ProductForSale(
    Guid Id,
    string Code,
    string Gtin,
    string Description,
    string Unit,
    decimal Price,
    string Ncm,
    string? Cest,
    int Origin,
    string Cfop,
    string Csosn,
    string PisCst,
    decimal PisRate,
    string CofinsCst,
    decimal CofinsRate,
    string IbsCbsCst,
    string ClassTrib);

/// <summary>Used by the Checkout module (through its own port) when a product is scanned.</summary>
public sealed class FindProductForSale(IProductRepository products, ITaxRuleRepository taxRules)
{
    public async Task<ProductForSale?> ExecuteAsync(string typed, CancellationToken cancellationToken = default)
    {
        var product = await FindProductByBarcode.FindActiveAsync(products, typed, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var rule = await taxRules.FindByIdAsync(product.TaxRuleId, cancellationToken)
            ?? throw new InvalidOperationException($"Tax rule {product.TaxRuleId} of product {product.Code} not found.");

        return new ProductForSale(
            product.Id, product.Code, product.Gtin.Value, product.Description, product.Unit, product.Price.Amount,
            product.Ncm.Value, product.Cest?.Value, (int)product.Origin,
            rule.Cfop.Value, rule.Csosn.Value,
            rule.PisCst.Value, rule.PisRate.Value, rule.CofinsCst.Value, rule.CofinsRate.Value,
            rule.TaxReform.IbsCbsCst, rule.TaxReform.ClassTrib);
    }
}
