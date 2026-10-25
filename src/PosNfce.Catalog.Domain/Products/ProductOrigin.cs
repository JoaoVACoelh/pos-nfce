namespace PosNfce.Catalog.Domain.Products;

/// <summary>Goods origin, NF-e <c>orig</c> tag (table "Origem da Mercadoria" of the layout).</summary>
public enum ProductOrigin
{
    Domestic = 0,
    ForeignDirectImport = 1,
    ForeignBoughtDomestically = 2,
    DomesticImportContentOver40UpTo70 = 3,
    DomesticBasicProductionProcesses = 4,
    DomesticImportContentUpTo40 = 5,
    ForeignDirectImportNoDomesticEquivalent = 6,
    ForeignBoughtDomesticallyNoDomesticEquivalent = 7,
    DomesticImportContentOver70 = 8,
}
