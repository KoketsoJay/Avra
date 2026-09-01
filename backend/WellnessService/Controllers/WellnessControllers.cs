using Microsoft.AspNetCore.Mvc;
using WellnessService.Models;
using WellnessService.Services;

namespace WellnessService.Controllers;

/// <summary>
/// API controller for wellness plan operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PlansController : ControllerBase
{
    private readonly IWellnessPlanService _planService;
    private readonly ILogger<PlansController> _logger;

    public PlansController(IWellnessPlanService planService, ILogger<PlansController> logger)
    {
        _planService = planService;
        _logger = logger;
    }

    /// <summary>
    /// Get all active wellness plans
    /// </summary>
    /// <returns>List of wellness plans</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WellnessPlanDto>>> GetAllPlans()
    {
        try
        {
            var plans = await _planService.GetAllPlansAsync();
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all wellness plans");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching plans" });
        }
    }

    /// <summary>
    /// Get wellness plan by ID
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <returns>Plan details</returns>
    [HttpGet("{planId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WellnessPlanDto>> GetPlan(string planId)
    {
        try
        {
            var plan = await _planService.GetPlanByIdAsync(planId);
            if (plan == null)
            {
                return NotFound(new { message = $"Plan with id {planId} not found" });
            }
            return Ok(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching plan with id {PlanId}", planId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching plan" });
        }
    }

    /// <summary>
    /// Get plans by category
    /// </summary>
    /// <param name="category">Plan category</param>
    /// <returns>List of plans in category</returns>
    [HttpGet("search/category")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WellnessPlanDto>>> GetPlansByCategory([FromQuery] string category)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return BadRequest(new { message = "Category is required" });
            }

            var plans = await _planService.GetPlansByCategoryAsync(category);
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching plans by category: {Category}", category);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error searching plans" });
        }
    }

    /// <summary>
    /// Get plans by level
    /// </summary>
    /// <param name="level">Plan level (Beginner, Intermediate, Advanced)</param>
    /// <returns>List of plans at level</returns>
    [HttpGet("search/level")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WellnessPlanDto>>> GetPlansByLevel([FromQuery] string level)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(level))
            {
                return BadRequest(new { message = "Level is required" });
            }

            var plans = await _planService.GetPlansByLevelAsync(level);
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching plans by level: {Level}", level);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error searching plans" });
        }
    }

    /// <summary>
    /// Create new wellness plan
    /// </summary>
    /// <param name="createDto">Plan creation data</param>
    /// <returns>Created plan</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WellnessPlanDto>> CreatePlan(CreateWellnessPlanDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var plan = await _planService.CreatePlanAsync(createDto);
            return CreatedAtAction(nameof(GetPlan), new { planId = plan.Id }, plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating wellness plan");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating plan" });
        }
    }

    /// <summary>
    /// Update wellness plan
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <param name="updateDto">Plan update data</param>
    /// <returns>Updated plan</returns>
    [HttpPut("{planId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WellnessPlanDto>> UpdatePlan(string planId, CreateWellnessPlanDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var plan = await _planService.UpdatePlanAsync(planId, updateDto);
            return Ok(plan);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Plan not found with id {PlanId}", planId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating plan with id {PlanId}", planId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating plan" });
        }
    }

    /// <summary>
    /// Delete wellness plan
    /// </summary>
    /// <param name="planId">Plan ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{planId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlan(string planId)
    {
        try
        {
            var deleted = await _planService.DeletePlanAsync(planId);
            if (!deleted)
            {
                return NotFound(new { message = $"Plan with id {planId} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting plan with id {PlanId}", planId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error deleting plan" });
        }
    }
}

/// <summary>
/// API controller for user progress operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProgressController : ControllerBase
{
    private readonly IProgressService _progressService;
    private readonly ILogger<ProgressController> _logger;

    public ProgressController(IProgressService progressService, ILogger<ProgressController> logger)
    {
        _progressService = progressService;
        _logger = logger;
    }

    /// <summary>
    /// Enroll user in wellness plan
    /// </summary>
    /// <param name="enrollDto">Enrollment data</param>
    /// <returns>User progress tracking record</returns>
    [HttpPost("enroll")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProgressDto>> EnrollInPlan(EnrollInPlanDto enrollDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var progress = await _progressService.EnrollInPlanAsync(enrollDto.UserId, enrollDto.PlanId);
            return CreatedAtAction(nameof(GetProgress), new { progressId = progress.Id }, progress);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Plan not found for enrollment");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enrolling user in plan");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error enrolling in plan" });
        }
    }

    /// <summary>
    /// Get user progress record by ID
    /// </summary>
    /// <param name="progressId">Progress ID</param>
    /// <returns>Progress details</returns>
    [HttpGet("{progressId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProgressDto>> GetProgress(string progressId)
    {
        try
        {
            var progress = await _progressService.GetProgressByIdAsync(progressId);
            if (progress == null)
            {
                return NotFound(new { message = $"Progress with id {progressId} not found" });
            }
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching progress with id {ProgressId}", progressId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching progress" });
        }
    }

    /// <summary>
    /// Get all progress records for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of user progress records</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserProgressDto>>> GetUserProgress(string userId)
    {
        try
        {
            var progresses = await _progressService.GetUserProgressesAsync(userId);
            return Ok(progresses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching progress for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching progress" });
        }
    }

    /// <summary>
    /// Update user progress
    /// </summary>
    /// <param name="progressId">Progress ID</param>
    /// <param name="updateDto">Progress update data</param>
    /// <returns>Updated progress</returns>
    [HttpPut("{progressId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProgressDto>> UpdateProgress(string progressId, UpdateProgressDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var progress = await _progressService.UpdateProgressAsync(progressId, updateDto);
            return Ok(progress);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Progress not found with id {ProgressId}", progressId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating progress with id {ProgressId}", progressId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating progress" });
        }
    }

    /// <summary>
    /// Complete wellness plan
    /// </summary>
    /// <param name="progressId">Progress ID</param>
    /// <returns>Success status</returns>
    [HttpPost("{progressId}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompletePlan(string progressId)
    {
        try
        {
            var completed = await _progressService.CompleteProgressAsync(progressId);
            if (!completed)
            {
                return NotFound(new { message = $"Progress with id {progressId} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing plan with progress id {ProgressId}", progressId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error completing plan" });
        }
    }
}
