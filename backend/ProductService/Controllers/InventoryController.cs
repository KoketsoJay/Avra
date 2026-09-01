using Avra.ServiceDefaults.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Models;
using ProductService.Services;

namespace ProductService.Controllers;

/// <summary>
/// Warehouse operations over the catalogue. Every endpoint is restricted to administrators —
/// stock levels and reorder settings are not shopper-facing data.
/// </summary>
[ApiController]
[Route("api/inventory")]
[Produces("application/json")]
[Authorize(Policy = AvraPolicies.AdministratorOnly)]
public class InventoryController(IProductService productService, ILogger<InventoryController> logger) : ControllerBase
{
    /// <summary>Full inventory position, de-listed products included.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetInventory() =>
        Ok(await productService.GetInventoryAsync());

    /// <summary>Products that have fallen to or below their reorder threshold.</summary>
    [HttpGet("low-stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetLowStock() =>
        Ok(await productService.GetLowStockAsync());

    /// <summary>Receives or writes off stock for a product.</summary>
    /// <param name="productId">Product ID.</param>
    /// <param name="adjustment">Signed quantity and the reason for the movement.</param>
    [HttpPost("{productId:int}/adjust")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryItemDto>> AdjustStock(int productId, StockAdjustmentDto adjustment)
    {
        try
        {
            return Ok(await productService.AdjustStockAsync(productId, adjustment));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Stock adjustment rejected for product {ProductId}", productId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Updates the reorder threshold and warehouse cap for a product.</summary>
    /// <param name="productId">Product ID.</param>
    /// <param name="thresholds">The new reorder settings.</param>
    [HttpPut("{productId:int}/thresholds")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryItemDto>> UpdateThresholds(int productId, StockThresholdsDto thresholds)
    {
        try
        {
            return Ok(await productService.UpdateThresholdsAsync(productId, thresholds));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Threshold update rejected for product {ProductId}", productId);
            return BadRequest(new { message = ex.Message });
        }
    }
}
