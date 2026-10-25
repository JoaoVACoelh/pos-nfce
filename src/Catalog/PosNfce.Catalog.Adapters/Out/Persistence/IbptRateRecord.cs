namespace PosNfce.Catalog.Adapters.Out.Persistence;

/// <summary>Row of <c>catalog_ibpt_rates</c>. Rates in percent.</summary>
public sealed class IbptRateRecord
{
    public int Id { get; set; }
    public string Ncm { get; set; } = "";
    public string Ex { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal FederalNational { get; set; }
    public decimal FederalImported { get; set; }
    public decimal State { get; set; }
    public decimal Municipal { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidUntil { get; set; }
    public string Version { get; set; } = "";
    public string Source { get; set; } = "";
}
