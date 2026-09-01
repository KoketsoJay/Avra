using Avra.ServiceDefaults.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Models;
using ProductService.Services;

namespace ProductService.Controllers;

/// <summary>
/// API controller for product operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Get all products
    /// </summary>
    /// <returns>List of all active products</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProducts()
    {
        try
        {
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all products");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching products" });
        }
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProductById(int id)
    {
        try
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound(new { message = $"Product with id {id} not found" });
            }
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product with id {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching product" });
        }
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    /// <param name="category">Category name</param>
    /// <returns>Products in specified category</returns>
    [HttpGet("category/{category}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsByCategory(string category)
    {
        try
        {
            var products = await _productService.GetProductsByCategoryAsync(category);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products in category {Category}", category);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching products" });
        }
    }

    /// <summary>
    /// Search products
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <returns>Products matching search criteria</returns>
    [HttpGet("search/{searchTerm}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> SearchProducts(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest(new { message = "Search term cannot be empty" });
            }

            var products = await _productService.SearchProductsAsync(searchTerm);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with term {SearchTerm}", searchTerm);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error searching products" });
        }
    }

    /// <summary>
    /// Create new product
    /// </summary>
    /// <param name="createDto">Product creation data</param>
    /// <returns>Created product</returns>
    [HttpPost]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = await _productService.CreateProductAsync(createDto);
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating product" });
        }
    }

    /// <summary>
    /// Update product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="updateDto">Product update data</param>
    /// <returns>Updated product</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, CreateProductDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var product = await _productService.UpdateProductAsync(id, updateDto);
            return Ok(product);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Product not found with id {ProductId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with id {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating product" });
        }
    }

    /// <summary>
    /// Delete product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Success or failure</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            var deleted = await _productService.DeleteProductAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Product with id {id} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with id {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error deleting product" });
        }
    }
}
