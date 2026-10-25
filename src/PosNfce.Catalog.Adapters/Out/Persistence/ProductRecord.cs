namespace PosNfce.Catalog.Adapters.Out.Persistence;

/// <summary>Row of <c>catalog_products</c>. Plain data: the rules live in the domain <c>Product</c>.</summary>
public sealed class ProductRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Gtin { get; set; } = "";
    public string Description { get; set; } = "";
    public string Ncm { get; set; } = "";
    public string? Cest { get; set; }
    public string Unit { get; set; } = "";
    public decimal Price { get; set; }
    public int Origin { get; set; }
    public Guid TaxRuleId { get; set; }
    public bool Active { get; set; }
}
