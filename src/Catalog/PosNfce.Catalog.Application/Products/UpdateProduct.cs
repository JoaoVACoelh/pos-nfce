using PosNfce.Catalog.Application.Ports;
using PosNfce.Catalog.Domain.Products;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Application.Products;

public sealed record UpdateProductCommand(
    Guid Id,
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
            command.Description,
            Ncm.Parse(command.Ncm),
            Cest.ParseOptional(command.Cest),
            command.Unit,
            new Money(command.Price),
            command.Origin,
            command.TaxRuleId);

        if (!await taxRules.ExistsAsync(product.TaxRuleId, cancellationToken))
        {
            throw new DomainException("A regra tributária informada não existe.");
        }

        await products.UpdateAsync(product, cancellationToken);
    }
}
