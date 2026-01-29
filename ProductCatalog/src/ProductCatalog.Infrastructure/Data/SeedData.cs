using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductCatalog.Core.Entities;

namespace ProductCatalog.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        // Ensure database is created and migrations are applied
        await context.Database.MigrateAsync();

        // Check if data already exists
        if (await context.Categories.AnyAsync())
        {
            logger.LogInformation("Database already seeded.");
            return;
        }

        logger.LogInformation("Seeding database.");

        var categories = GetCategories();
        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        var products = GetProducts(categories);
        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();

        logger.LogInformation("Database seeded with {categoryCount} categories and {productCount} products.",
            categories.Count, products.Count);
    }

    private static List<Category> GetCategories()
    {
        return new List<Category>
        {
            new() { Name = "Electronics", Description = "Electronic devices and accessories" },
            new() { Name = "Clothing", Description = "Apparel and fashion items" },
            new() { Name = "Books", Description = "Physical and digital books" },
            new() { Name = "Home & Garden", Description = "Home improvement and garden supplies" },
            new() { Name = "Sports & Outdoors", Description = "Sporting goods and outdoor equipment" }
        };
    }

    private static List<Product> GetProducts(List<Category> categories)
    {
        var electronics = categories.First(c => c.Name == "Electronics");
        var clothing = categories.First(c => c.Name == "Clothing");
        var books = categories.First(c => c.Name == "Books");
        var homeGarden = categories.First(c => c.Name == "Home & Garden");
        var sports = categories.First(c => c.Name == "Sports & Outdoors");

        return new List<Product>
        {
            // Electronics (5 products)
            new() { Name = "Wireless Bluetooth Headphones", Description = "High-quality noise-canceling headphones", Price = 149.99m, StockQuantity = 50, Category = electronics },
            new() { Name = "USB-C Charging Cable", Description = "Fast charging cable, 6ft length", Price = 12.99m, StockQuantity = 200, Category = electronics },
            new() { Name = "Portable Power Bank", Description = "10000mAh portable charger", Price = 29.99m, StockQuantity = 75, Category = electronics },
            new() { Name = "Wireless Mouse", Description = "Ergonomic wireless mouse with adjustable DPI", Price = 24.99m, StockQuantity = 100, Category = electronics },
            new() { Name = "Smart Watch", Description = "Fitness tracking smartwatch with heart rate monitor", Price = 199.99m, StockQuantity = 0, Category = electronics }, // Out of stock

            // Clothing (4 products)
            new() { Name = "Cotton T-Shirt", Description = "100% organic cotton, available in multiple colors", Price = 19.99m, StockQuantity = 150, Category = clothing },
            new() { Name = "Denim Jeans", Description = "Classic fit denim jeans", Price = 49.99m, StockQuantity = 80, Category = clothing },
            new() { Name = "Running Shoes", Description = "Lightweight running shoes with cushioned sole", Price = 89.99m, StockQuantity = 45, Category = clothing },
            new() { Name = "Winter Jacket", Description = "Insulated waterproof winter jacket", Price = 129.99m, StockQuantity = 0, Category = clothing }, // Out of stock

            // Books (4 products)
            new() { Name = "Clean Code", Description = "A Handbook of Agile Software Craftsmanship by Robert C. Martin", Price = 39.99m, StockQuantity = 30, Category = books },
            new() { Name = "Design Patterns", Description = "Elements of Reusable Object-Oriented Software", Price = 54.99m, StockQuantity = 25, Category = books },
            new() { Name = "The Pragmatic Programmer", Description = "Your Journey to Mastery, 20th Anniversary Edition", Price = 49.99m, StockQuantity = 40, Category = books },
            new() { Name = "Domain-Driven Design", Description = "Tackling Complexity in the Heart of Software", Price = 59.99m, StockQuantity = 15, Category = books },

            // Home & Garden (4 products)
            new() { Name = "Garden Hose", Description = "50ft expandable garden hose with spray nozzle", Price = 34.99m, StockQuantity = 60, Category = homeGarden },
            new() { Name = "LED Desk Lamp", Description = "Adjustable LED desk lamp with USB charging port", Price = 42.99m, StockQuantity = 85, Category = homeGarden },
            new() { Name = "Tool Set", Description = "130-piece household tool kit", Price = 79.99m, StockQuantity = 35, Category = homeGarden },
            new() { Name = "Plant Pot Set", Description = "Set of 5 ceramic plant pots in various sizes", Price = 29.99m, StockQuantity = 0, Category = homeGarden }, // Out of stock

            // Sports & Outdoors (3 products)
            new() { Name = "Yoga Mat", Description = "Non-slip yoga mat, 6mm thick", Price = 24.99m, StockQuantity = 120, Category = sports },
            new() { Name = "Camping Tent", Description = "2-person waterproof camping tent", Price = 89.99m, StockQuantity = 20, Category = sports },
            new() { Name = "Resistance Bands Set", Description = "Set of 5 resistance bands with different strengths", Price = 19.99m, StockQuantity = 90, Category = sports }
        };
    }
}
