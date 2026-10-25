using PosNfce.Catalog.Domain.Products;

namespace PosNfce.Desktop.Catalog.Products;

/// <summary>Goods origin as the operator reads it, with the code from the NF-e layout table.</summary>
public sealed record OriginOption(ProductOrigin Value, string Label)
{
    public static IReadOnlyList<OriginOption> All { get; } =
    [
        new(ProductOrigin.Domestic, "0 - Nacional"),
        new(ProductOrigin.ForeignDirectImport, "1 - Estrangeira, importação direta"),
        new(ProductOrigin.ForeignBoughtDomestically, "2 - Estrangeira, adquirida no mercado interno"),
        new(ProductOrigin.DomesticImportContentOver40UpTo70, "3 - Nacional, conteúdo de importação acima de 40% e até 70%"),
        new(ProductOrigin.DomesticBasicProductionProcesses, "4 - Nacional, processos produtivos básicos"),
        new(ProductOrigin.DomesticImportContentUpTo40, "5 - Nacional, conteúdo de importação até 40%"),
        new(ProductOrigin.ForeignDirectImportNoDomesticEquivalent, "6 - Estrangeira, importação direta, sem similar nacional"),
        new(ProductOrigin.ForeignBoughtDomesticallyNoDomesticEquivalent, "7 - Estrangeira, mercado interno, sem similar nacional"),
        new(ProductOrigin.DomesticImportContentOver70, "8 - Nacional, conteúdo de importação acima de 70%"),
    ];
}
