using Avra.ServiceDefaults.Security;
using EventsService.Models;
using EventsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventsService.Controllers;

/// <summary>
/// Promotion publishing. Shoppers only ever see live promotions; drafting, publishing and
/// archiving are administrator operations.
/// </summary>
[ApiController]
[Route("api/promotions")]
[Produces("application/json")]
public class PromotionsController(IPromotionService promotionService, ILogger<PromotionsController> logger) : ControllerBase
{
    /// <summary>Promotions currently running.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PromotionDto>>> GetLivePromotions() =>
        Ok(await promotionService.GetLivePromotionsAsync());

    /// <summary>Every promotion including drafts. Administrators only.</summary>
    [HttpGet("admin")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PromotionDto>>> GetAllPromotions() =>
        Ok(await promotionService.GetAllPromotionsAsync());

    /// <summary>Gets a single promotion.</summary>
    [HttpGet("{promotionId}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromotionDto>> GetPromotion(string promotionId)
    {
        var promotion = await promotionService.GetPromotionByIdAsync(promotionId);
        return promotion is null
            ? NotFound(new { message = $"Promotion with id {promotionId} not found" })
            : Ok(promotion);
    }

    /// <summary>Drafts a new promotion. Administrators only.</summary>
    [HttpPost]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PromotionDto>> CreatePromotion(CreatePromotionDto createDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var promotion = await promotionService.CreatePromotionAsync(createDto);
            return CreatedAtAction(nameof(GetPromotion), new { promotionId = promotion.Id }, promotion);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Promotion rejected");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Updates a promotion. Administrators only.</summary>
    [HttpPut("{promotionId}")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PromotionDto>> UpdatePromotion(string promotionId, CreatePromotionDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            return Ok(await promotionService.UpdatePromotionAsync(promotionId, updateDto));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Promotion update rejected for {PromotionId}", promotionId);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Publishes a promotion so shoppers can see it. Administrators only.</summary>
    [HttpPut("{promotionId}/publish")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<PromotionDto>> PublishPromotion(string promotionId) =>
        ChangeStatusAsync(promotionId, PromotionStatus.Published);

    /// <summary>Takes a promotion back off the storefront. Administrators only.</summary>
    [HttpPut("{promotionId}/unpublish")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<PromotionDto>> UnpublishPromotion(string promotionId) =>
        ChangeStatusAsync(promotionId, PromotionStatus.Draft);

    /// <summary>Archives a promotion. Administrators only.</summary>
    [HttpPut("{promotionId}/archive")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<PromotionDto>> ArchivePromotion(string promotionId) =>
        ChangeStatusAsync(promotionId, PromotionStatus.Archived);

    /// <summary>Deletes a promotion outright. Administrators only.</summary>
    [HttpDelete("{promotionId}")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePromotion(string promotionId) =>
        await promotionService.DeletePromotionAsync(promotionId)
            ? NoContent()
            : NotFound(new { message = $"Promotion with id {promotionId} not found" });

    private async Task<ActionResult<PromotionDto>> ChangeStatusAsync(string promotionId, PromotionStatus status)
    {
        try
        {
            return Ok(await promotionService.SetStatusAsync(promotionId, status));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Promotion status change rejected for {PromotionId}", promotionId);
            return BadRequest(new { message = ex.Message });
        }
    }
}
