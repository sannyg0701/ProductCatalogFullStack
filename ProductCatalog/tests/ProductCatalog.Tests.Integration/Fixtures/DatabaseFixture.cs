using Microsoft.EntityFrameworkCore;
using ProductCatalog.Infrastructure.Data;
using Testcontainers.MsSql;

namespace ProductCatalog.Tests.Integration;

[SetUpFixture]
public class DatabaseFixture
{
    private static MsSqlContainer _container = null!;
    public static string ConnectionString { get; private set; } = string.Empty;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        // Generate password at runtime to avoid tripping secret scanners
        var password = $"Test@{Guid.NewGuid():N}";

        _container = new MsSqlBuilder()
            .WithPassword(password)
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        // Create database and seed data
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
        await SeedTestData(context);
    }

    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
        await _container.DisposeAsync();
    }

    private static async Task SeedTestData(ApplicationDbContext context)
    {
        // Add test categories
        var electronics = new Core.Entities.Category
        {
            Name = "Electronics",
            Description = "Electronic devices",
            IsActive = true
        };
        var clothing = new Core.Entities.Category
        {
            Name = "Clothing",
            Description = "Apparel and accessories",
            IsActive = true
        };
        var inactive = new Core.Entities.Category
        {
            Name = "Inactive Category",
            Description = "This category is inactive",
            IsActive = false
        };

        context.Categories.AddRange(electronics, clothing, inactive);
        await context.SaveChangesAsync();

        // Add test products with varied data for search/filter testing
        var products = new List<Core.Entities.Product>
        {
            new() { Name = "Laptop", Description = "High-performance laptop", Price = 999.99m, CategoryId = electronics.Id, StockQuantity = 10, CreatedDate = DateTime.UtcNow, IsActive = true },
            new() { Name = "Smartphone", Description = "Latest smartphone model", Price = 699.99m, CategoryId = electronics.Id, StockQuantity = 25, CreatedDate = DateTime.UtcNow, IsActive = true },
            new() { Name = "Headphones", Description = "Wireless headphones", Price = 149.99m, CategoryId = electronics.Id, StockQuantity = 50, CreatedDate = DateTime.UtcNow, IsActive = true },
            new() { Name = "T-Shirt", Description = "Cotton t-shirt", Price = 29.99m, CategoryId = clothing.Id, StockQuantity = 100, CreatedDate = DateTime.UtcNow, IsActive = true },
            new() { Name = "Jeans", Description = "Blue denim jeans", Price = 59.99m, CategoryId = clothing.Id, StockQuantity = 75, CreatedDate = DateTime.UtcNow, IsActive = true },
            new() { Name = "Sneakers", Description = "Running sneakers", Price = 89.99m, CategoryId = clothing.Id, StockQuantity = 0, CreatedDate = DateTime.UtcNow, IsActive = true }, // Out of stock
            new() { Name = "Inactive Product", Description = "This product is inactive", Price = 19.99m, CategoryId = electronics.Id, StockQuantity = 5, CreatedDate = DateTime.UtcNow, IsActive = false },
            // Product with special characters for LIKE escaping test
            new() { Name = "50% Off Item", Description = "Special sale_item with 50% discount", Price = 49.99m, CategoryId = clothing.Id, StockQuantity = 20, CreatedDate = DateTime.UtcNow, IsActive = true },
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }
}
