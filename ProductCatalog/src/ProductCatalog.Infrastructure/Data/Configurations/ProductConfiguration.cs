using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductCatalog.Core.Entities;

namespace ProductCatalog.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.StockQuantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Foreign key relationship
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for query optimization

        // FK index - improves JOIN performance
        builder.HasIndex(p => p.CategoryId)
            .HasDatabaseName("IX_Product_CategoryId");

        // Filtered index for active products (most common filter)
        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_Product_IsActive");

        // Index for price filtering and sorting
        builder.HasIndex(p => p.Price)
            .HasDatabaseName("IX_Product_Price");

        // Index for name search
        builder.HasIndex(p => p.Name)
            .HasDatabaseName("IX_Product_Name");

        // Index for sorting by created date
        builder.HasIndex(p => p.CreatedDate)
            .HasDatabaseName("IX_Product_CreatedDate");

        // Composite index for common search pattern: active products by category
        builder.HasIndex(p => new { p.IsActive, p.CategoryId })
            .HasDatabaseName("IX_Product_IsActive_CategoryId");
    }
}
