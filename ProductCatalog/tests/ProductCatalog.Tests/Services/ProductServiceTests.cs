using Microsoft.Extensions.Logging;
using Moq;
using ProductCatalog.Core.DTOs.Requests;
using ProductCatalog.Core.DTOs.Responses;
using ProductCatalog.Core.Entities;
using ProductCatalog.Core.Interfaces.Repositories;
using ProductCatalog.Infrastructure.Services;

namespace ProductCatalog.Tests.Services;

[TestFixture]
public class ProductServiceTests
{
    private Mock<IProductRepository> _productRepositoryMock;
    private Mock<ICategoryRepository> _categoryRepositoryMock;
    private Mock<ILogger<ProductService>> _loggerMock;
    private ProductService _productService;

    [SetUp]
    public void SetUp()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _loggerMock = new Mock<ILogger<ProductService>>();
        _productService = new ProductService(
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task GetAllActiveAsync_WhenCalled_ReturnsProductsFromRepository()
    {
        var expectedProducts = new List<ProductResponse>
        {
            new() { Id = 1, Name = "Product 1", CategoryName = "Category 1" },
            new() { Id = 2, Name = "Product 2", CategoryName = "Category 1" }
        };
        _productRepositoryMock
            .Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProducts);

        var result = await _productService.GetAllActiveAsync();

        Assert.That(result, Is.EqualTo(expectedProducts));
    }

    [Test]
    public async Task GetByIdAsync_WhenProductExists_ReturnsProduct()
    {
        var expectedProduct = new ProductResponse { Id = 1, Name = "Test Product", CategoryName = "Test Category" };
        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProduct);

        var result = await _productService.GetByIdAsync(1);

        Assert.That(result, Is.EqualTo(expectedProduct));
    }

    [Test]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ReturnsNull()
    {
        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductResponse?)null);

        var result = await _productService.GetByIdAsync(999);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task SearchAsync_WhenCalled_ReturnsPagedResultFromRepository()
    {
        var request = new ProductSearchRequest { SearchTerm = "test", PageNumber = 1, PageSize = 10 };
        var expectedResult = new PagedResult<ProductResponse>
        {
            Items = new List<ProductResponse> { new() { Id = 1, Name = "Test Product" } },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };
        _productRepositoryMock
            .Setup(r => r.SearchAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await _productService.SearchAsync(request);

        Assert.That(result, Is.EqualTo(expectedResult));
    }

    [Test]
    public async Task CreateAsync_WhenCategoryExists_CreatesAndReturnsProduct()
    {
        var request = new CreateProductRequest
        {
            Name = "New Product",
            Description = "Description",
            Price = 29.99m,
            CategoryId = 1,
            StockQuantity = 100
        };
        var category = new Category { Id = 1, Name = "Electronics", IsActive = true };
        var createdProduct = new Product
        {
            Id = 1,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            CategoryId = request.CategoryId,
            StockQuantity = request.StockQuantity,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };

        _categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(request.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _productRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdProduct);

        var result = await _productService.CreateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(createdProduct.Id));
            Assert.That(result.Name, Is.EqualTo(createdProduct.Name));
            Assert.That(result.CategoryName, Is.EqualTo(category.Name));
        });
    }

    [Test]
    public void CreateAsync_WhenCategoryDoesNotExist_ThrowsInvalidOperationException()
    {
        var request = new CreateProductRequest
        {
            Name = "New Product",
            Price = 29.99m,
            CategoryId = 999,
            StockQuantity = 100
        };
        _categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _productService.CreateAsync(request));

        Assert.That(exception.Message, Does.Contain("999"));
    }

    [Test]
    public async Task UpdateAsync_WhenProductExistsAndCategoryValid_UpdatesAndReturnsProduct()
    {
        var existingProduct = new Product
        {
            Id = 1,
            Name = "Old Name",
            CategoryId = 1,
            Price = 19.99m,
            StockQuantity = 50,
            IsActive = true
        };
        var request = new UpdateProductRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Price = 29.99m,
            CategoryId = 1,
            StockQuantity = 100
        };
        var expectedResponse = new ProductResponse
        {
            Id = 1,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            CategoryId = request.CategoryId,
            CategoryName = "Electronics",
            StockQuantity = request.StockQuantity
        };

        _productRepositoryMock
            .Setup(r => r.GetEntityByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);
        _categoryRepositoryMock
            .Setup(r => r.ExistsAsync(request.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _productService.UpdateAsync(1, request);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo(request.Name));
            Assert.That(result.Price, Is.EqualTo(request.Price));
        });
    }

    [Test]
    public async Task UpdateAsync_WhenProductDoesNotExist_ReturnsNull()
    {
        var request = new UpdateProductRequest
        {
            Name = "Updated Name",
            Price = 29.99m,
            CategoryId = 1,
            StockQuantity = 100
        };
        _productRepositoryMock
            .Setup(r => r.GetEntityByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _productService.UpdateAsync(999, request);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void UpdateAsync_WhenChangingToNonExistentCategory_ThrowsInvalidOperationException()
    {
        var existingProduct = new Product
        {
            Id = 1,
            Name = "Product",
            CategoryId = 1,
            IsActive = true
        };
        var request = new UpdateProductRequest
        {
            Name = "Updated Name",
            Price = 29.99m,
            CategoryId = 999,
            StockQuantity = 100
        };

        _productRepositoryMock
            .Setup(r => r.GetEntityByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);
        _categoryRepositoryMock
            .Setup(r => r.ExistsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _productService.UpdateAsync(1, request));

        Assert.That(exception.Message, Does.Contain("999"));
    }

    [Test]
    public async Task DeleteAsync_WhenProductExists_SoftDeletesAndReturnsTrue()
    {
        var existingProduct = new Product
        {
            Id = 1,
            Name = "Product",
            IsActive = true
        };
        _productRepositoryMock
            .Setup(r => r.GetEntityByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        var result = await _productService.DeleteAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(existingProduct.IsActive, Is.False);
        });
        _productRepositoryMock.Verify(
            r => r.UpdateAsync(existingProduct, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DeleteAsync_WhenProductDoesNotExist_ReturnsFalse()
    {
        _productRepositoryMock
            .Setup(r => r.GetEntityByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _productService.DeleteAsync(999);

        Assert.That(result, Is.False);
    }
}
