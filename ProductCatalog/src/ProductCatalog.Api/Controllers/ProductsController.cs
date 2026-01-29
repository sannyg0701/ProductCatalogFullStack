using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Core.DTOs.Requests;
using ProductCatalog.Core.DTOs.Responses;
using ProductCatalog.Core.Interfaces.Services;

namespace ProductCatalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService,
        ILogger<ProductsController> logger)
    {
        ArgumentNullException.ThrowIfNull(productService);
        ArgumentNullException.ThrowIfNull(logger);
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all active products.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting all active products.");
        IReadOnlyList<ProductResponse> products = await _productService.GetAllActiveAsync(cancellationToken);
        return Ok(products);
    }

    /// <summary>
    /// Gets a product by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting product with id {productId}.", id);
        ProductResponse? product = await _productService.GetByIdAsync(id, cancellationToken);

        if (product is null)
        {
            _logger.LogDebug("Product with id {productId} not found.", id);
            return NotFound(new ProblemDetails
            {
                Title = "Product not found",
                Detail = $"Product with ID {id} was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(product);
    }

    /// <summary>
    /// Searches products with filtering, sorting, and pagination.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProductResponse>>> Search(
        [FromQuery] ProductSearchRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Searching products with term {searchTerm}.", request.SearchTerm);
        PagedResult<ProductResponse> result = await _productService.SearchAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating product with name {productName}.", request.Name);

        try
        {
            ProductResponse product = await _productService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create product: {message}.", ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid operation",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponse>> Update(
        int id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating product with id {productId}.", id);

        try
        {
            ProductResponse? product = await _productService.UpdateAsync(id, request, cancellationToken);

            if (product is null)
            {
                _logger.LogDebug("Product with id {productId} not found for update.", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Product not found",
                    Detail = $"Product with ID {id} was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update product {productId}: {message}.", id, ex.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid operation",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Deletes a product (soft delete).
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Deleting product with id {productId}.", id);
        bool deleted = await _productService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            _logger.LogDebug("Product with id {productId} not found for deletion.", id);
            return NotFound(new ProblemDetails
            {
                Title = "Product not found",
                Detail = $"Product with ID {id} was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return NoContent();
    }
}
