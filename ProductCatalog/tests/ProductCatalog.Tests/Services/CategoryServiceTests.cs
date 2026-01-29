using Microsoft.Extensions.Logging;
using Moq;
using ProductCatalog.Core.DTOs.Requests;
using ProductCatalog.Core.Entities;
using ProductCatalog.Core.Interfaces.Repositories;
using ProductCatalog.Infrastructure.Services;

namespace ProductCatalog.Tests.Services;

[TestFixture]
public class CategoryServiceTests
{
    private Mock<ICategoryRepository> _categoryRepositoryMock;
    private Mock<ILogger<CategoryService>> _loggerMock;
    private CategoryService _categoryService;

    [SetUp]
    public void SetUp()
    {
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _loggerMock = new Mock<ILogger<CategoryService>>();
        _categoryService = new CategoryService(
            _categoryRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task GetAllActiveAsync_WhenCalled_ReturnsCategoryResponsesFromRepository()
    {
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Electronics", Description = "Electronic items", IsActive = true },
            new() { Id = 2, Name = "Clothing", Description = "Apparel", IsActive = true }
        };
        _categoryRepositoryMock
            .Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var result = await _categoryService.GetAllActiveAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Id, Is.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("Electronics"));
            Assert.That(result[1].Id, Is.EqualTo(2));
            Assert.That(result[1].Name, Is.EqualTo("Clothing"));
        });
    }

    [Test]
    public async Task GetAllActiveAsync_WhenNoCategories_ReturnsEmptyList()
    {
        _categoryRepositoryMock
            .Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        var result = await _categoryService.GetAllActiveAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task CreateAsync_WhenCalled_CreatesAndReturnsCategoryResponse()
    {
        var request = new CreateCategoryRequest
        {
            Name = "New Category",
            Description = "New category description"
        };
        var createdCategory = new Category
        {
            Id = 1,
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };
        _categoryRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdCategory);

        var result = await _categoryService.CreateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(1));
            Assert.That(result.Name, Is.EqualTo(request.Name));
            Assert.That(result.Description, Is.EqualTo(request.Description));
            Assert.That(result.IsActive, Is.True);
        });
    }

    [Test]
    public async Task CreateAsync_WhenDescriptionIsNull_CreatesWithNullDescription()
    {
        var request = new CreateCategoryRequest
        {
            Name = "Category Without Description",
            Description = null
        };
        var createdCategory = new Category
        {
            Id = 1,
            Name = request.Name,
            Description = null,
            IsActive = true
        };
        _categoryRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdCategory);

        var result = await _categoryService.CreateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(request.Name));
            Assert.That(result.Description, Is.Null);
        });
    }
}
