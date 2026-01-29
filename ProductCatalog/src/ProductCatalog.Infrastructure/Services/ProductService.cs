using Microsoft.Extensions.Logging;
using ProductCatalog.Core.DTOs.Requests;
using ProductCatalog.Core.DTOs.Responses;
using ProductCatalog.Core.Entities;
using ProductCatalog.Core.Interfaces.Repositories;
using ProductCatalog.Core.Interfaces.Services;

namespace ProductCatalog.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ILogger<ProductService> logger)
    {
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(categoryRepository);
        ArgumentNullException.ThrowIfNull(logger);
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all active products.");
        return await _productRepository.GetAllActiveAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving product with id {productId}.", id);
        return await _productRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<ProductResponse>> SearchAsync(
        ProductSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogDebug("Searching products with term {searchTerm}.", request.SearchTerm);
        return await _productRepository.SearchAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate category exists
        var categoryExists = await _categoryRepository.ExistsAsync(request.CategoryId, cancellationToken)
            .ConfigureAwait(false);

        if (!categoryExists)
        {
            _logger.LogWarning("Attempted to create product with non-existent category {categoryId}.", request.CategoryId);
            throw new InvalidOperationException($"Category with ID {request.CategoryId} does not exist.");
        }

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            CategoryId = request.CategoryId,
            StockQuantity = request.StockQuantity,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };

        var createdProduct = await _productRepository.AddAsync(product, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Created product with id {productId}.", createdProduct.Id);

        // Fetch the full response with category name
        var response = await _productRepository.GetByIdAsync(createdProduct.Id, cancellationToken)
            .ConfigureAwait(false);

        return response!;
    }

    public async Task<ProductResponse?> UpdateAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await _productRepository.GetEntityByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (product is null)
        {
            _logger.LogDebug("Product with id {productId} not found for update.", id);
            return null;
        }

        // Validate category exists if changing category
        if (product.CategoryId != request.CategoryId)
        {
            var categoryExists = await _categoryRepository.ExistsAsync(request.CategoryId, cancellationToken)
                .ConfigureAwait(false);

            if (!categoryExists)
            {
                _logger.LogWarning("Attempted to update product {productId} with non-existent category {categoryId}.", id, request.CategoryId);
                throw new InvalidOperationException($"Category with ID {request.CategoryId} does not exist.");
            }
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.CategoryId = request.CategoryId;
        product.StockQuantity = request.StockQuantity;

        await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Updated product with id {productId}.", id);

        // Fetch the full response with category name
        return await _productRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetEntityByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (product is null)
        {
            _logger.LogDebug("Product with id {productId} not found for deletion.", id);
            return false;
        }

        // Soft delete
        product.IsActive = false;
        await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Soft-deleted product with id {productId}.", id);

        return true;
    }
}
