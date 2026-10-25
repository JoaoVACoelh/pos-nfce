using PosNfce.Checkout.Application.Ports;
using PosNfce.Checkout.Domain.Sales;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Application.Sales;

/// <summary>Scans a product into the open sale, opening a new sale on the first item.</summary>
public sealed class AddItemToSale(ISaleRepository sales, IProductCatalog catalog, TimeProvider clock)
{
    public async Task<SaleView> ExecuteAsync(string typed, CancellationToken cancellationToken = default)
    {
        var input = ScanInput.Parse(typed);
        var product = await catalog.FindAsync(input.Code, cancellationToken)
            ?? throw new DomainException($"Produto \"{input.Code}\" não encontrado.");

        var sale = await sales.GetOpenAsync(cancellationToken) ?? Sale.Open(BrazilTime.Now(clock));
        sale.AddItem(product.Id, product.Code, product.Gtin, product.Description, product.Unit,
            new Money(product.Price), input.Quantity, product.Tax);

        await sales.SaveAsync(sale, cancellationToken);
        return SaleView.From(sale);
    }
}
