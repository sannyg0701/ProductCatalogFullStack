using ProductCatalog.Core.DTOs.Requests;
using ProductCatalog.Core.DTOs.Responses;

namespace ProductCatalog.Core.Interfaces.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
}
