using PosNfce.Catalog.Domain.Products;

namespace PosNfce.Catalog.Application.Ports;

public interface IProductRepository
{
    Task<Product?> FindByGtinAsync(Gtin gtin, CancellationToken cancellationToken = default);

    Task<Product?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);
}
