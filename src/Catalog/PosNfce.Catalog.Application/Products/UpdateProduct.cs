using PosNfce.Catalog.Application.Ports;
using PosNfce.Catalog.Domain.Products;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Application.Products;

public sealed record UpdateProductCommand(
    Guid Id,
    string? Gtin,
    string Description,
    string Ncm,
    string? Cest,
    string Unit,
    decimal Price,
    ProductOrigin Origin,
    Guid TaxRuleId);

public sealed class UpdateProduct(IProductRepository products, ITaxRuleRepository taxRules)
{
    public async Task ExecuteAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        var product = await products.FindByIdAsync(command.Id, cancellationToken)
            ?? throw new DomainException("Produto não encontrado.");

        product.Update(
            Gtin.Parse(command.Gtin),
            command.Description,
            Ncm.Parse(command.Ncm),
            Cest.ParseOptional(command.Cest),
            command.Unit,
            new Money(command.Price),
            command.Origin,
            command.TaxRuleId);

        // The new barcode can't belong to another product: the scanner would find the wrong one.
        if (product.Gtin.HasBarcode
            && await products.FindByGtinAsync(product.Gtin, cancellationToken) is { } other && other.Id != product.Id)
        {
            throw new DomainException($"O GTIN {product.Gtin} já é do produto \"{other.Description}\".");
        }

        if (!await taxRules.ExistsAsync(product.TaxRuleId, cancellationToken))
        {
            throw new DomainException("A regra tributária informada não existe.");
        }

        await products.UpdateAsync(product, cancellationToken);
    }
}
