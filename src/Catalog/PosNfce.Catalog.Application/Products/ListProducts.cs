using PosNfce.Catalog.Application.Ports;
using PosNfce.Catalog.Domain.Products;

namespace PosNfce.Catalog.Application.Products;

/// <summary>A product as the registration screen shows and edits it.</summary>
public sealed record ProductDetails(
    Guid Id,
    string Code,
    string Gtin,
    string Description,
    string Ncm,
    string? Cest,
    string Unit,
    decimal Price,
    ProductOrigin Origin,
    Guid TaxRuleId,
    string TaxRuleName,
    bool Active);

public sealed class ListProducts(IProductRepository products, ITaxRuleRepository taxRules)
{
    public const int MaxResults = 500;

    public async Task<IReadOnlyList<ProductDetails>> ExecuteAsync(string? search, CancellationToken cancellationToken = default)
    {
        var found = await products.SearchAsync(search?.Trim(), MaxResults, cancellationToken);
        var ruleNames = (await taxRules.ListAsync(cancellationToken)).ToDictionary(r => r.Id, r => r.Name);

        return found
            .OrderBy(p => p.Description, StringComparer.CurrentCultureIgnoreCase)
            .Select(p => new ProductDetails(
                p.Id, p.Code, p.Gtin.Value, p.Description, p.Ncm.Value, p.Cest?.Value, p.Unit, p.Price.Amount,
                p.Origin, p.TaxRuleId, ruleNames.GetValueOrDefault(p.TaxRuleId, "?"), p.Active))
            .ToList();
    }
}
