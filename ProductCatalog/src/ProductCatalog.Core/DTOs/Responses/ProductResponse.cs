namespace ProductCatalog.Core.DTOs.Responses;

public record ProductResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
    public DateTime CreatedDate { get; init; }
    public bool IsActive { get; init; }
}
