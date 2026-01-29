using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProductCatalog.Core.DTOs.Requests;
using ProductCatalog.Core.DTOs.Responses;
using ProductCatalog.Core.Entities;
using ProductCatalog.Core.Interfaces.Repositories;
using ProductCatalog.Infrastructure.Data;

namespace ProductCatalog.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                StockQuantity = p.StockQuantity,
                CreatedDate = p.CreatedDate,
                IsActive = p.IsActive
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(p => p.Id == id && p.IsActive)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                StockQuantity = p.StockQuantity,
                CreatedDate = p.CreatedDate,
                IsActive = p.IsActive
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Product?> GetEntityByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Searches products using a single SQL query with CTEs and UNION ALL.
    /// Returns correct TotalCount even when the requested page is empty (beyond last page).
    /// This satisfies the "efficient single-query implementation" requirement.
    /// </summary>
    public async Task<PagedResult<ProductResponse>> SearchAsync(
        ProductSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var parameters = new List<SqlParameter>();
        var whereConditions = new List<string> { "p.IsActive = 1" };

        // Search term filter (case-insensitive using SQL LOWER for consistent locale handling)
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var terms = request.SearchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < terms.Length; i++)
            {
                var paramName = $"@searchTerm{i}";
                var escapedTerm = EscapeLikeTerm(terms[i]);
                parameters.Add(new SqlParameter(paramName, $"%{escapedTerm}%"));
                whereConditions.Add($"(LOWER(p.Name) LIKE LOWER({paramName}) ESCAPE '\\' OR LOWER(p.Description) LIKE LOWER({paramName}) ESCAPE '\\')");
            }
        }

        // Category filter
        if (request.CategoryId.HasValue)
        {
            parameters.Add(new SqlParameter("@categoryId", request.CategoryId.Value));
            whereConditions.Add("p.CategoryId = @categoryId");
        }

        // Price range filters
        if (request.MinPrice.HasValue)
        {
            parameters.Add(new SqlParameter("@minPrice", request.MinPrice.Value));
            whereConditions.Add("p.Price >= @minPrice");
        }

        if (request.MaxPrice.HasValue)
        {
            parameters.Add(new SqlParameter("@maxPrice", request.MaxPrice.Value));
            whereConditions.Add("p.Price <= @maxPrice");
        }

        // In stock filter
        if (request.InStock.HasValue)
        {
            whereConditions.Add(request.InStock.Value ? "p.StockQuantity > 0" : "p.StockQuantity = 0");
        }

        // Build ORDER BY clause (whitelist approach to prevent SQL injection)
        // Uses fp alias to match the PagedProducts CTE scope
        var orderByClause = (request.SortBy?.ToLowerInvariant(), request.SortOrder?.ToLowerInvariant()) switch
        {
            ("price", "desc") => "fp.Price DESC, fp.Id ASC",
            ("price", _) => "fp.Price ASC, fp.Id ASC",
            ("createddate", "desc") => "fp.CreatedDate DESC, fp.Id ASC",
            ("createddate", _) => "fp.CreatedDate ASC, fp.Id ASC",
            ("stockquantity", "desc") => "fp.StockQuantity DESC, fp.Id ASC",
            ("stockquantity", _) => "fp.StockQuantity ASC, fp.Id ASC",
            ("name", "desc") => "fp.Name DESC, fp.Id ASC",
            _ => "fp.Name ASC, fp.Id ASC"
        };

        // Pagination parameters
        var offset = (request.PageNumber - 1) * request.PageSize;
        parameters.Add(new SqlParameter("@offset", offset));
        parameters.Add(new SqlParameter("@pageSize", request.PageSize));

        var whereClause = string.Join(" AND ", whereConditions);

        // Single query using CTEs to ensure TotalCount is correct even when page is empty.
        // Uses UNION ALL with a count-only row (IsCountRow=1) when no data rows exist.
        var sql = $@"
            WITH FilteredProducts AS (
                SELECT
                    p.Id, p.Name, p.Description, p.Price, p.CategoryId,
                    c.Name AS CategoryName, p.StockQuantity, p.CreatedDate, p.IsActive
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.Id
                WHERE {whereClause}
            ),
            TotalCount AS (
                SELECT COUNT(*) AS Cnt FROM FilteredProducts
            ),
            PagedProducts AS (
                SELECT
                    fp.Id, fp.Name, fp.Description, fp.Price, fp.CategoryId, fp.CategoryName,
                    fp.StockQuantity, fp.CreatedDate, fp.IsActive,
                    (SELECT Cnt FROM TotalCount) AS TotalCount,
                    CAST(0 AS BIT) AS IsCountRow
                FROM FilteredProducts fp
                ORDER BY {orderByClause}
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY
            )
            SELECT * FROM PagedProducts
            UNION ALL
            SELECT 0, '', NULL, 0, 0, '', 0, GETUTCDATE(), CAST(0 AS BIT),
                   (SELECT Cnt FROM TotalCount), CAST(1 AS BIT)
            WHERE (SELECT COUNT(*) FROM PagedProducts) = 0";

        // Execute raw SQL and map to internal DTO
        var results = await _context.Database
            .SqlQueryRaw<SearchResultRow>(sql, parameters.ToArray())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // TotalCount comes from any row (data rows or count-only row)
        var totalCount = results.FirstOrDefault()?.TotalCount ?? 0;

        // Filter out the count-only row when building items
        var items = results
            .Where(r => !r.IsCountRow)
            .Select(r => new ProductResponse
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Price = r.Price,
                CategoryId = r.CategoryId,
                CategoryName = r.CategoryName,
                StockQuantity = r.StockQuantity,
                CreatedDate = r.CreatedDate,
                IsActive = r.IsActive
            }).ToList();

        return new PagedResult<ProductResponse>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    private static string EscapeLikeTerm(string term)
    {
        return term
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal)
            .Replace("[", @"\[", StringComparison.Ordinal);
    }

    /// <summary>
    /// Internal DTO for raw SQL search results including total count.
    /// IsCountRow indicates a dummy row used only to return TotalCount when the page is empty.
    /// </summary>
    private sealed class SearchResultRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public int TotalCount { get; set; }
        public bool IsCountRow { get; set; }
    }

    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return product;
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(p => p.Id == id && p.IsActive, cancellationToken)
            .ConfigureAwait(false);
    }

}
