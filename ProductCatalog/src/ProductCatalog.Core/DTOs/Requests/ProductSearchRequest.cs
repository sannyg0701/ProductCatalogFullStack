using System.ComponentModel.DataAnnotations;

namespace ProductCatalog.Core.DTOs.Requests;

public class ProductSearchRequest
{
    public string? SearchTerm { get; init; }

    public int? CategoryId { get; init; }

    [Range(0, double.MaxValue, ErrorMessage = "MinPrice cannot be negative.")]
    public decimal? MinPrice { get; init; }

    [Range(0, double.MaxValue, ErrorMessage = "MaxPrice cannot be negative.")]
    public decimal? MaxPrice { get; init; }

    public bool? InStock { get; init; }

    public string? SortBy { get; init; }

    public string? SortOrder { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be at least 1.")]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; init; } = 10;
}
