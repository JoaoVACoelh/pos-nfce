using PosNfce.Catalog.Application.Products;
using PosNfce.Catalog.Application.TaxRules;
using PosNfce.Catalog.Domain.Products;

namespace PosNfce.Catalog.Tests;

/// <summary>
/// Fictitious data for tests only. The tax-looking values are format examples, not real classifications:
/// real tax data comes from the accountant.
/// </summary>
internal static class ProductSamples
{
    public static readonly Guid TaxRuleId = Guid.Parse("8f9b1c2e-0000-4000-8000-000000000001");

    public static RegisterTaxRuleCommand TaxRuleCommand(string name = "Regra de teste") => new(
        Name: name,
        Cfop: "5102",
        Csosn: "102",
        PisCst: "49",
        PisRate: 0m,
        CofinsCst: "49",
        CofinsRate: 0m,
        IbsCbsCst: "000",
        ClassTrib: "000001");

    public static RegisterProductCommand Command(string code = "001", string? gtin = "7891234567895", Guid? taxRuleId = null) => new(
        Code: code,
        Gtin: gtin,
        Description: "Produto de teste",
        Ncm: "00000000",
        Cest: null,
        Unit: "un",
        Price: 9.90m,
        Origin: ProductOrigin.Domestic,
        TaxRuleId: taxRuleId ?? TaxRuleId);
}
