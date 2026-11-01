namespace PosNfce.Catalog.Adapters.Out.Persistence;

/// <summary>Row of <c>catalog_tax_rules</c>. Plain data: the rules live in the domain <c>TaxRule</c>.</summary>
public sealed class TaxRuleRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Cfop { get; set; } = "";
    public string Csosn { get; set; } = "";
    public string PisCst { get; set; } = "";
    public decimal PisRate { get; set; }
    public string CofinsCst { get; set; } = "";
    public decimal CofinsRate { get; set; }
    public string IbsCbsCst { get; set; } = "";
    public string ClassTrib { get; set; } = "";
    public decimal IbsCbsReduction { get; set; }
}
