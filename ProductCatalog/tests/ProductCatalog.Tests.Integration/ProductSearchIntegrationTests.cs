using Microsoft.EntityFrameworkCore;
using ProductCatalog.Core.DTOs.Requests;
using ProductCatalog.Infrastructure.Data;
using ProductCatalog.Infrastructure.Repositories;

namespace ProductCatalog.Tests.Integration;

[TestFixture]
public class ProductSearchIntegrationTests
{
    private ApplicationDbContext _context = null!;
    private ProductRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(DatabaseFixture.ConnectionString)
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ProductRepository(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task SearchAsync_WithNoFilters_ReturnsActiveProducts()
    {
        var request = new ProductSearchRequest { PageNumber = 1, PageSize = 10 };

        var result = await _repository.SearchAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Is.Not.Empty);
            Assert.That(result.Items.All(p => p.IsActive), Is.True);
            Assert.That(result.TotalCount, Is.GreaterThan(0));
        });
    }

    [Test]
    public async Task SearchAsync_WithSearchTerm_ReturnsMatchingProducts()
    {
        var request = new ProductSearchRequest
        {
            SearchTerm = "laptop",
            PageNumber = 1,
            PageSize = 10
        };

        var result = await _repository.SearchAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items[0].Name, Is.EqualTo("Laptop"));
            Assert.That(result.TotalCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task SearchAsync_WithCategoryFilter_ReturnsProductsInCategory()
    {
        // First get the Electronics category ID
        var electronics = await _context.Categories.FirstAsync(c => c.Name == "Electronics");

        var request = new ProductSearchRequest
        {
            CategoryId = electronics.Id,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await _repository.SearchAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Is.Not.Empty);
            Assert.That(result.Items.All(p => p.CategoryId == electronics.Id), Is.True);
        });
    }

    [Test]
    public async Task SearchAsync_WithPriceRange_ReturnsProductsInRange()
    {
        var request = new ProductSearchRequest
        {
            MinPrice = 50m,
            MaxPrice = 200m,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await _repository.SearchAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Is.Not.Empty);
            Assert.That(result.Items.All(p => p.Price >= 50m && p.Price <= 200m), Is.True);
        });
    }

    [Test]
    public async Task SearchAsync_WithInStockFilter_ReturnsOnlyInStockProducts()
    {
        var request = new ProductSearchRequest
        {
            InStock = true,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await _repository.SearchAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Is.Not.Empty);
            Assert.That(result.Items.All(p => p.StockQuantity > 0), Is.True);
        });
    }

    [Test]
    public async Task SearchAsync_WithPagination_ReturnsCorrectPage()
    {
        var request = new ProductSearchRequest
        {
            PageNumber = 1,
            PageSize = 2
        };

        var result = await _repository.SearchAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.LessThanOrEqualTo(2));
            Assert.That(result.PageNumber, Is.EqualTo(1));
            Assert.That(result.PageSize, Is.EqualTo(2));
            Assert.That(result.TotalCount, Is.GreaterThan(2)); // We have more than 2 products
        });
    }

    [Test]
    public async Task SearchAsync_PageBeyondLastPage_ReturnsTotalCountCorrectly()
    {
        // Request a page that doesn't exist
        var request = new ProductSearchRequest
        {
            PageNumber = 100,
            PageSize = 10
        };

        var result = await _repository.SearchAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Is.Empty);
            Assert.That(result.TotalCount, Is.GreaterThan(0), "TotalCount should reflect actual count even when page is empty");
        });
    }

    [Test]
    public async Task SearchAsync_WithSpecialCharacters_TreatsThemLiterally()
    {
        // Search for "50%" - should find the "50% Off Item" product
        // The % should be treated literally, not as a wildcard
        var request = new ProductSearchRequest
        {
            SearchTerm = "50%",
            PageNumber = 1,
            PageSize = 10
        };

        var result = await _repository.SearchAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items[0].Name, Does.Contain("50%"));
        });
    }

    [Test]
    public async Task SearchAsync_WithUnderscoreCharacter_TreatsItLiterally()
    {
        // Search for "sale_item" - the _ should be literal, not a single-char wildcard
        var request = new ProductSearchRequest
        {
            SearchTerm = "sale_item",
            PageNumber = 1,
            PageSize = 10
        };

        var result = await _repository.SearchAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items[0].Description, Does.Contain("sale_item"));
        });
    }

    [Test]
    public async Task SearchAsync_WithSorting_ReturnsSortedResults()
    {
        var request = new ProductSearchRequest
        {
            SortBy = "price",
            SortOrder = "asc",
            PageNumber = 1,
            PageSize = 10
        };

        var result = await _repository.SearchAsync(request);

        Assert.That(result.Items, Is.Not.Empty);

        var prices = result.Items.Select(p => p.Price).ToList();
        Assert.That(prices, Is.EqualTo(prices.OrderBy(p => p).ToList()), "Results should be sorted by price ascending");
    }

    [Test]
    public async Task SearchAsync_WithMultipleFilters_CombinesCorrectly()
    {
        var electronics = await _context.Categories.FirstAsync(c => c.Name == "Electronics");

        var request = new ProductSearchRequest
        {
            CategoryId = electronics.Id,
            MinPrice = 100m,
            InStock = true,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await _repository.SearchAsync(request);

        Assert.That(result.Items.All(p =>
            p.CategoryId == electronics.Id &&
            p.Price >= 100m &&
            p.StockQuantity > 0), Is.True);
    }
}
