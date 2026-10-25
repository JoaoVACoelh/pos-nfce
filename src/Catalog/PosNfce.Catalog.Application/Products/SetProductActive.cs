using PosNfce.Catalog.Application.Ports;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Application.Products;

/// <summary>
/// Products are deactivated, never deleted: past sales and issued documents still point to them.
/// An inactive product is not found at the checkout.
/// </summary>
public sealed class SetProductActive(IProductRepository products)
{
    public async Task ExecuteAsync(Guid id, bool active, CancellationToken cancellationToken = default)
    {
        var product = await products.FindByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Produto não encontrado.");

        if (active)
        {
            product.Activate();
        }
        else
        {
            product.Deactivate();
        }

        await products.UpdateAsync(product, cancellationToken);
    }
}
