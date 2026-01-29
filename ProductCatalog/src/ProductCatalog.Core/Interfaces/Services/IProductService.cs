using ProductCatalog.Core.DTOs.Requests;
using ProductCatalog.Core.DTOs.Responses;

namespace ProductCatalog.Core.Interfaces.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<ProductResponse>> SearchAsync(
        ProductSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductResponse?> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
