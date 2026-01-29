using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ProductCatalog.Api.Controllers;
using ProductCatalog.Core.DTOs.Requests;
using ProductCatalog.Core.DTOs.Responses;
using ProductCatalog.Core.Interfaces.Services;

namespace ProductCatalog.Tests.Controllers;

[TestFixture]
public class ProductsControllerTests
{
    private Mock<IProductService> _productServiceMock;
    private Mock<ILogger<ProductsController>> _loggerMock;
    private ProductsController _controller;

    [SetUp]
    public void SetUp()
    {
        _productServiceMock = new Mock<IProductService>();
        _loggerMock = new Mock<ILogger<ProductsController>>();
        _controller = new ProductsController(
            _productServiceMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task GetAll_WhenCalled_ReturnsOkWithProducts()
    {
        var products = new List<ProductResponse>
        {
            new() { Id = 1, Name = "Product 1" },
            new() { Id = 2, Name = "Product 2" }
        };
        _productServiceMock
            .Setup(s => s.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = result.Result as OkObjectResult;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(okResult.Value, Is.EqualTo(products));
        });
    }

    [Test]
    public async Task GetById_WhenProductExists_ReturnsOkWithProduct()
    {
        var product = new ProductResponse { Id = 1, Name = "Test Product" };
        _productServiceMock
            .Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _controller.GetById(1, CancellationToken.None);

        var okResult = result.Result as OkObjectResult;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(okResult.Value, Is.EqualTo(product));
        });
    }

    [Test]
    public async Task GetById_WhenProductDoesNotExist_ReturnsNotFound()
    {
        _productServiceMock
            .Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductResponse?)null);

        var result = await _controller.GetById(999, CancellationToken.None);

        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.Multiple(() =>
        {
            Assert.That(notFoundResult, Is.Not.Null);
            Assert.That(notFoundResult!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
            var problemDetails = notFoundResult.Value as ProblemDetails;
            Assert.That(problemDetails, Is.Not.Null);
            Assert.That(problemDetails!.Title, Is.EqualTo("Product not found"));
            Assert.That(problemDetails.Status, Is.EqualTo(StatusCodes.Status404NotFound));
        });
    }

    [Test]
    public async Task Search_WhenCalled_ReturnsOkWithPagedResult()
    {
        var request = new ProductSearchRequest { SearchTerm = "test" };
        var pagedResult = new PagedResult<ProductResponse>
        {
            Items = new List<ProductResponse> { new() { Id = 1, Name = "Test" } },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };
        _productServiceMock
            .Setup(s => s.SearchAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.Search(request, CancellationToken.None);

        var okResult = result.Result as OkObjectResult;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.Value, Is.EqualTo(pagedResult));
        });
    }

    [Test]
    public async Task Create_WhenValid_ReturnsCreatedAtActionWithProduct()
    {
        var request = new CreateProductRequest
        {
            Name = "New Product",
            Price = 29.99m,
            CategoryId = 1,
            StockQuantity = 100
        };
        var createdProduct = new ProductResponse
        {
            Id = 1,
            Name = request.Name,
            Price = request.Price,
            CategoryId = request.CategoryId,
            StockQuantity = request.StockQuantity
        };
        _productServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdProduct);

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Result as CreatedAtActionResult;
        Assert.Multiple(() =>
        {
            Assert.That(createdResult, Is.Not.Null);
            Assert.That(createdResult!.StatusCode, Is.EqualTo(StatusCodes.Status201Created));
            Assert.That(createdResult.ActionName, Is.EqualTo(nameof(ProductsController.GetById)));
            Assert.That(createdResult.Value, Is.EqualTo(createdProduct));
        });
    }

    [Test]
    public async Task Create_WhenCategoryDoesNotExist_ReturnsBadRequest()
    {
        var request = new CreateProductRequest
        {
            Name = "New Product",
            Price = 29.99m,
            CategoryId = 999,
            StockQuantity = 100
        };
        _productServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Category with ID 999 does not exist."));

        var result = await _controller.Create(request, CancellationToken.None);

        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.Multiple(() =>
        {
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        });
    }

    [Test]
    public async Task Update_WhenProductExists_ReturnsOkWithUpdatedProduct()
    {
        var request = new UpdateProductRequest
        {
            Name = "Updated Product",
            Price = 39.99m,
            CategoryId = 1,
            StockQuantity = 150
        };
        var updatedProduct = new ProductResponse
        {
            Id = 1,
            Name = request.Name,
            Price = request.Price
        };
        _productServiceMock
            .Setup(s => s.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedProduct);

        var result = await _controller.Update(1, request, CancellationToken.None);

        var okResult = result.Result as OkObjectResult;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.Value, Is.EqualTo(updatedProduct));
        });
    }

    [Test]
    public async Task Update_WhenProductDoesNotExist_ReturnsNotFound()
    {
        var request = new UpdateProductRequest
        {
            Name = "Updated Product",
            Price = 39.99m,
            CategoryId = 1,
            StockQuantity = 150
        };
        _productServiceMock
            .Setup(s => s.UpdateAsync(999, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductResponse?)null);

        var result = await _controller.Update(999, request, CancellationToken.None);

        var notFoundResult = result.Result as NotFoundObjectResult;
        Assert.Multiple(() =>
        {
            Assert.That(notFoundResult, Is.Not.Null);
            Assert.That(notFoundResult!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
            var problemDetails = notFoundResult.Value as ProblemDetails;
            Assert.That(problemDetails, Is.Not.Null);
            Assert.That(problemDetails!.Title, Is.EqualTo("Product not found"));
            Assert.That(problemDetails.Status, Is.EqualTo(StatusCodes.Status404NotFound));
        });
    }

    [Test]
    public async Task Update_WhenCategoryDoesNotExist_ReturnsBadRequest()
    {
        var request = new UpdateProductRequest
        {
            Name = "Updated Product",
            Price = 39.99m,
            CategoryId = 999,
            StockQuantity = 150
        };
        _productServiceMock
            .Setup(s => s.UpdateAsync(1, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Category with ID 999 does not exist."));

        var result = await _controller.Update(1, request, CancellationToken.None);

        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
    }

    [Test]
    public async Task Delete_WhenProductExists_ReturnsNoContent()
    {
        _productServiceMock
            .Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Delete(1, CancellationToken.None);

        Assert.That(result, Is.TypeOf<NoContentResult>());
    }

    [Test]
    public async Task Delete_WhenProductDoesNotExist_ReturnsNotFound()
    {
        _productServiceMock
            .Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.Delete(999, CancellationToken.None);

        var notFoundResult = result as NotFoundObjectResult;
        Assert.Multiple(() =>
        {
            Assert.That(notFoundResult, Is.Not.Null);
            Assert.That(notFoundResult!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
            var problemDetails = notFoundResult.Value as ProblemDetails;
            Assert.That(problemDetails, Is.Not.Null);
            Assert.That(problemDetails!.Title, Is.EqualTo("Product not found"));
            Assert.That(problemDetails.Status, Is.EqualTo(StatusCodes.Status404NotFound));
        });
    }

}
