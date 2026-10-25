using PosNfce.Catalog.Domain.Products;

namespace PosNfce.Catalog.Application.Ports;

public interface IProductRepository
{
    Task<Product?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Product?> FindByGtinAsync(Gtin gtin, CancellationToken cancellationToken = default);

    Task<Product?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>Products whose code, GTIN or description contains <paramref name="term"/>; all when it is empty.</summary>
    Task<IReadOnlyList<Product>> SearchAsync(string? term, int limit, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
}
