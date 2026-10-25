using PosNfce.Catalog.Application.Products;
using PosNfce.Checkout.Application.Ports;
using PosNfce.Checkout.Domain.Sales;

namespace PosNfce.Checkout.Adapters.Out.Catalog;

/// <summary>Implements Checkout's catalog port with the Catalog module's own use case: no access to Catalog's tables.</summary>
public sealed class CatalogProductLookup(FindProductForSale findProduct) : IProductCatalog
{
    public async Task<ProductToSell?> FindAsync(string code, CancellationToken cancellationToken = default)
    {
        var p = await findProduct.ExecuteAsync(code, cancellationToken);

        return p is null
            ? null
            : new ProductToSell(p.Id, p.Code, p.Gtin, p.Description, p.Unit, p.Price, new TaxSnapshot(
                p.Ncm, p.Cest, p.Origin, p.Cfop, p.Csosn, p.PisCst, p.PisRate, p.CofinsCst, p.CofinsRate, p.IbsCbsCst, p.ClassTrib));
    }
}
