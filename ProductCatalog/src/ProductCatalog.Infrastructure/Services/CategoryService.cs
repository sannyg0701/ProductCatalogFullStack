using Microsoft.Extensions.Logging;
using ProductCatalog.Core.DTOs.Requests;
using ProductCatalog.Core.DTOs.Responses;
using ProductCatalog.Core.Entities;
using ProductCatalog.Core.Interfaces.Repositories;
using ProductCatalog.Core.Interfaces.Services;

namespace ProductCatalog.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ICategoryRepository categoryRepository,
        ILogger<CategoryService> logger)
    {
        ArgumentNullException.ThrowIfNull(categoryRepository);
        ArgumentNullException.ThrowIfNull(logger);
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all active categories.");

        var categories = await _categoryRepository.GetAllActiveAsync(cancellationToken);

        return categories.Select(c => new CategoryResponse
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            IsActive = c.IsActive
        }).ToList();
    }

    public async Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };

        var createdCategory = await _categoryRepository.AddAsync(category, cancellationToken);
        _logger.LogDebug("Created category with id {categoryId}.", createdCategory.Id);

        return new CategoryResponse
        {
            Id = createdCategory.Id,
            Name = createdCategory.Name,
            Description = createdCategory.Description,
            IsActive = createdCategory.IsActive
        };
    }
}
