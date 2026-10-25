using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Domain.Taxes;

/// <summary>Which ICMS group of the NF-e layout carries the item for a Simples Nacional issuer.</summary>
public enum SimplesIcmsGroup
{
    /// <summary><c>ICMSSN102</c>: CSOSN 102, 103, 300 and 400 (only origin and CSOSN).</summary>
    Sn102,

    /// <summary><c>ICMSSN500</c>: CSOSN 500, ICMS charged earlier by tax substitution.</summary>
    Sn500,
}

public sealed record SimplesIcms(SimplesIcmsGroup Group, string Csosn, int Origin);

/// <summary>Which PIS/COFINS group of the layout the CST maps to.</summary>
public enum PisCofinsGroup
{
    /// <summary>CST 01 and 02: base × rate (<c>PISAliq</c> / <c>COFINSAliq</c>).</summary>
    Rate,

    /// <summary>CST 04 to 09: not taxed at this sale, only the CST (<c>PISNT</c> / <c>COFINSNT</c>).</summary>
    NotTaxed,

    /// <summary>CST 49 to 99: other operations (<c>PISOutr</c> / <c>COFINSOutr</c>).</summary>
    Other,
}

public sealed record PisCofins(PisCofinsGroup Group, string Cst, Money Base, decimal Rate, Money Value);

/// <summary>Approximate tax rates of the item (IBPT table), in percent.</summary>
public sealed record ApproximateRates(decimal Federal, decimal State, decimal Municipal)
{
    public decimal Total => Federal + State + Municipal;
}

/// <summary>Everything the NFC-e needs about the taxes of one item.</summary>
public sealed record ItemTaxes(SimplesIcms Icms, PisCofins Pis, PisCofins Cofins, Money ApproximateTax);
