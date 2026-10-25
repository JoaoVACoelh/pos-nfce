using PosNfce.Catalog.Application.Ports;
using PosNfce.Catalog.Domain.Products;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Application.Products;

/// <summary>Raw values typed in the product screen; the use case turns them into domain types.</summary>
public sealed record RegisterProductCommand(
    string Code,
    string? Gtin,
    string Description,
    string Ncm,
    string? Cest,
    string Unit,
    decimal Price,
    ProductOrigin Origin,
    Guid TaxRuleId);

public sealed class RegisterProduct(IProductRepository products)
{
    public async Task<Guid> ExecuteAsync(RegisterProductCommand command, CancellationToken cancellationToken = default)
    {
        var product = Product.Register(
            command.Code,
            Gtin.Parse(command.Gtin),
            command.Description,
            Ncm.Parse(command.Ncm),
            Cest.ParseOptional(command.Cest),
            command.Unit,
            new Money(command.Price),
            command.Origin,
            command.TaxRuleId);

        // Uniqueness needs the database, so it is checked here and not inside the entity.
        if (await products.FindByCodeAsync(product.Code, cancellationToken) is not null)
        {
            throw new DomainException($"Já existe um produto com o código \"{product.Code}\".");
        }

        if (product.Gtin.HasBarcode && await products.FindByGtinAsync(product.Gtin, cancellationToken) is not null)
        {
            throw new DomainException($"Já existe um produto com o GTIN {product.Gtin}.");
        }

        await products.AddAsync(product, cancellationToken);
        return product.Id;
    }
}
