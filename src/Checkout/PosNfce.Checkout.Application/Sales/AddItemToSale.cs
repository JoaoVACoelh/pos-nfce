using System.Globalization;
using PosNfce.Checkout.Application.Ports;
using PosNfce.Checkout.Domain.Sales;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Application.Sales;

/// <summary>Scans a product into the open sale, opening a new sale on the first item.</summary>
public sealed class AddItemToSale(ISaleRepository sales, IProductCatalog catalog, TimeProvider clock)
{
    private const int MaxScaleCodeDigits = 6;

    public async Task<SaleView> ExecuteAsync(string typed, CancellationToken cancellationToken = default)
    {
        var input = ScanInput.Parse(typed);
        var (product, quantity) = await FindAsync(input, cancellationToken);

        var sale = await sales.GetOpenAsync(cancellationToken) ?? Sale.Open(BrazilTime.Now(clock));
        sale.AddItem(product.Id, product.Code, product.Gtin, product.Description, product.Unit,
            new Money(product.Price), quantity, product.Tax);

        await sales.SaveAsync(sale, cancellationToken);
        return SaleView.From(sale);
    }

    /// <summary>
    /// GTIN or internal code first, so a product registered with an in-store barcode starting with 2 still wins;
    /// then a scale label, whose weight becomes the quantity.
    /// </summary>
    private async Task<(ProductToSell Product, decimal Quantity)> FindAsync(ScanInput input, CancellationToken cancellationToken)
    {
        var product = await catalog.FindAsync(input.Code, cancellationToken)
            ?? await FindBySameNumberAsync(input.Code, cancellationToken);
        if (product is not null)
        {
            return (product, input.QuantityFor(product.Unit, product.Description));
        }

        if (!ScaleLabel.TryParse(input.Code, out var label))
        {
            throw new DomainException($"Produto \"{input.Code}\" não encontrado.");
        }

        if (input.Quantity != 1)
        {
            throw new DomainException("A etiqueta da balança já traz o peso: leia a etiqueta sem quantidade.");
        }

        product = await catalog.FindByScaleCodeAsync(label.ItemCode, cancellationToken)
            ?? throw new DomainException(
                $"Etiqueta da balança com o código {label.ItemCode}: nenhum produto ativo tem esse código interno.");

        return (product, label.QuantityFor(product.Unit, product.Description));
    }

    /// <summary>"5" typed by hand finds the product "000005", the same match a scale label gets.</summary>
    private Task<ProductToSell?> FindBySameNumberAsync(string code, CancellationToken cancellationToken) =>
        code.Length <= MaxScaleCodeDigits && code.All(char.IsAsciiDigit)
            ? catalog.FindByScaleCodeAsync(int.Parse(code, CultureInfo.InvariantCulture), cancellationToken)
            : Task.FromResult<ProductToSell?>(null);
}
