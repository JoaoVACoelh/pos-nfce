namespace PosNfce.Checkout.Domain.Sales;

/// <summary>
/// Copy of the product's tax data at the moment it was sold. Checkout only stores it (as text, the way the
/// NF-e layout carries it); the Fiscal module validates and calculates. Editing the product later never changes a sale.
/// </summary>
public sealed record TaxSnapshot(
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
    string ClassTrib,
    decimal IbsCbsReduction);
