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
public class CategoriesControllerTests
{
    private Mock<ICategoryService> _categoryServiceMock;
    private Mock<ILogger<CategoriesController>> _loggerMock;
    private CategoriesController _controller;

    [SetUp]
    public void SetUp()
    {
        _categoryServiceMock = new Mock<ICategoryService>();
        _loggerMock = new Mock<ILogger<CategoriesController>>();
        _controller = new CategoriesController(
            _categoryServiceMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task GetAll_WhenCalled_ReturnsOkWithCategories()
    {
        var categories = new List<CategoryResponse>
        {
            new() { Id = 1, Name = "Electronics", IsActive = true },
            new() { Id = 2, Name = "Clothing", IsActive = true }
        };
        _categoryServiceMock
            .Setup(s => s.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = result.Result as OkObjectResult;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(okResult.Value, Is.EqualTo(categories));
        });
    }

    [Test]
    public async Task GetAll_WhenNoCategories_ReturnsOkWithEmptyList()
    {
        _categoryServiceMock
            .Setup(s => s.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryResponse>());

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = result.Result as OkObjectResult;
        var categories = okResult?.Value as IReadOnlyList<CategoryResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.Not.Null);
            Assert.That(categories, Is.Empty);
        });
    }

    [Test]
    public async Task Create_WhenValid_ReturnsCreatedAtActionWithCategory()
    {
        var request = new CreateCategoryRequest
        {
            Name = "New Category",
            Description = "New category description"
        };
        var createdCategory = new CategoryResponse
        {
            Id = 1,
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };
        _categoryServiceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdCategory);

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = result.Result as CreatedAtActionResult;
        Assert.Multiple(() =>
        {
            Assert.That(createdResult, Is.Not.Null);
            Assert.That(createdResult!.StatusCode, Is.EqualTo(StatusCodes.Status201Created));
            Assert.That(createdResult.ActionName, Is.EqualTo(nameof(CategoriesController.GetAll)));
            Assert.That(createdResult.Value, Is.EqualTo(createdCategory));
        });
    }

}
