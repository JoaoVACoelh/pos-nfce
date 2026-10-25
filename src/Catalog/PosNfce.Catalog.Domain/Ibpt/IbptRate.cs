using PosNfce.Catalog.Domain.Products;
using PosNfce.Catalog.Domain.TaxRules;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Domain.Ibpt;

/// <summary>
/// Approximate tax burden of one NCM, from the IBPT table (Lei 12.741/2012). The receipt shows
/// "Val. aprox. tributos" using these rates; they are estimates published by IBPT, not the taxes actually calculated.
/// </summary>
public sealed record IbptRate
{
    public Ncm Ncm { get; }

    /// <summary>TIPI exception code ("ex"), usually empty. The same NCM may appear once per exception.</summary>
    public string Ex { get; }

    public string Description { get; }

    /// <summary>Federal taxes for domestic goods (origins 0, 3, 4, 5 and 8).</summary>
    public Percentage FederalNational { get; }

    /// <summary>Federal taxes for imported goods (origins 1, 2, 6 and 7).</summary>
    public Percentage FederalImported { get; }

    public Percentage State { get; }
    public Percentage Municipal { get; }
    public DateOnly ValidFrom { get; }
    public DateOnly ValidUntil { get; }
    public string Version { get; }
    public string Source { get; }

    public IbptRate(
        Ncm ncm, string? ex, string description,
        Percentage federalNational, Percentage federalImported, Percentage state, Percentage municipal,
        DateOnly validFrom, DateOnly validUntil, string version, string source)
    {
        if (validUntil < validFrom)
        {
            throw new DomainException($"Vigência inválida no NCM {ncm}: termina antes de começar.");
        }

        Ncm = ncm;
        Ex = ex?.Trim() ?? "";
        Description = description.Trim();
        FederalNational = federalNational;
        FederalImported = federalImported;
        State = state;
        Municipal = municipal;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        Version = version.Trim();
        Source = source.Trim();
    }

    /// <summary>Federal rate that applies to a product, depending on whether its origin is domestic or foreign.</summary>
    public Percentage FederalFor(ProductOrigin origin) => origin switch
    {
        ProductOrigin.ForeignDirectImport
            or ProductOrigin.ForeignBoughtDomestically
            or ProductOrigin.ForeignDirectImportNoDomesticEquivalent
            or ProductOrigin.ForeignBoughtDomesticallyNoDomesticEquivalent => FederalImported,
        _ => FederalNational,
    };
}
