using ProductCatalog.Core.DTOs.Requests;
using ProductCatalog.Core.DTOs.Responses;
using ProductCatalog.Core.Entities;

namespace ProductCatalog.Core.Interfaces.Repositories;

public interface IProductRepository
{
    Task<IReadOnlyList<ProductResponse>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Product?> GetEntityByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<ProductResponse>> SearchAsync(
        ProductSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);

    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
