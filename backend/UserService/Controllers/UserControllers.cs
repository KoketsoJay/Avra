using Avra.ServiceDefaults.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using UserService.Services;

namespace UserService.Controllers;

/// <summary>
/// API controller for authentication operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="registerDto">Registration data</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterDto registerDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _userService.RegisterAsync(registerDto);
            if (result == null)
            {
                return BadRequest(new { message = "User with this email already exists" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error during registration" });
        }
    }

    /// <summary>
    /// Login user
    /// </summary>
    /// <param name="loginDto">Login credentials</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginDto loginDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _userService.LoginAsync(loginDto);
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error during login" });
        }
    }
}

/// <summary>
/// API controller for user profile operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Search active members. Administrators only — used to pick who receives a discount.
    /// </summary>
    /// <param name="term">Matches email, first name or last name. Omit to list the first page.</param>
    [HttpGet("search")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> SearchUsers([FromQuery] string? term)
    {
        try
        {
            return Ok(await _userService.SearchUsersAsync(term));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error searching users" });
        }
    }

    /// <summary>
    /// Get user profile by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User profile details</returns>
    [HttpGet("{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(string userId)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = $"User with id {userId} not found" });
            }
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user with id {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching user" });
        }
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="updateDto">Profile update data</param>
    /// <returns>Updated user profile</returns>
    [HttpPut("{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UpdateProfile(string userId, UpdateProfileDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userService.UpdateProfileAsync(userId, updateDto);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found with id {UserId}", userId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for id {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating profile" });
        }
    }

    /// <summary>
    /// Delete user account
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        try
        {
            var deleted = await _userService.DeleteUserAsync(userId);
            if (!deleted)
            {
                return NotFound(new { message = $"User with id {userId} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user with id {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error deleting user" });
        }
    }
}

/// <summary>
/// API controller for membership operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MembershipsController : ControllerBase
{
    private readonly IMembershipService _membershipService;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<MembershipsController> _logger;

    public MembershipsController(IMembershipService membershipService, ICurrentUser currentUser, ILogger<MembershipsController> logger)
    {
        _membershipService = membershipService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Get the membership tiers offered on the storefront
    /// </summary>
    /// <returns>Active membership plans in display order</returns>
    [HttpGet("plans")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MembershipPlanDto>>> GetPlans()
    {
        try
        {
            return Ok(await _membershipService.GetPlansAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching membership plans");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching membership plans" });
        }
    }

    /// <summary>
    /// Set the discount every member on a tier receives. Administrators only.
    /// </summary>
    /// <param name="planId">Membership plan id.</param>
    /// <param name="dto">The new rate, 0-100.</param>
    [HttpPut("plans/{planId}/discount")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MembershipPlanDto>> SetPlanDiscount(string planId, [FromBody] SetPlanDiscountDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            var plan = await _membershipService.SetPlanDiscountAsync(planId, dto.DiscountPercentage);
            if (plan is null)
            {
                return NotFound(new { message = $"Membership plan {planId} not found" });
            }

            return Ok(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting discount for plan {PlanId}", planId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error setting plan discount" });
        }
    }

    /// <summary>
    /// The discount that applies to the signed-in shopper right now. Drives storefront pricing
    /// and is re-resolved server-side at checkout, so it is safe for the client to read.
    /// </summary>
    [HttpGet("discount/me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<EffectiveDiscountDto>> GetMyDiscount()
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _membershipService.GetEffectiveDiscountAsync(userId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving discount for the signed-in user");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error resolving discount" });
        }
    }

    /// <summary>
    /// Every member who holds a discount override. Administrators only.
    /// </summary>
    [HttpGet("discounts")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MemberDiscountDto>>> GetMemberDiscounts()
    {
        try
        {
            return Ok(await _membershipService.GetMemberDiscountsAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching member discounts");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching member discounts" });
        }
    }

    /// <summary>
    /// Grant or update one member's discount override. Administrators only.
    /// </summary>
    /// <param name="userId">The member receiving the discount.</param>
    /// <param name="grant">Rate, reason and optional expiry.</param>
    [HttpPut("discounts/{userId}")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDiscountDto>> GrantMemberDiscount(string userId, [FromBody] GrantMemberDiscountDto grant)
    {
        ArgumentNullException.ThrowIfNull(grant);

        try
        {
            var result = await _membershipService.GrantMemberDiscountAsync(userId, grant, _currentUser.UserId ?? "unknown");
            if (result is null)
            {
                return NotFound(new { message = $"User with id {userId} not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting discount to {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error granting discount" });
        }
    }

    /// <summary>
    /// Remove a member's discount override, dropping them back to their tier rate. Administrators only.
    /// </summary>
    /// <param name="userId">The member losing the override.</param>
    [HttpDelete("discounts/{userId}")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeMemberDiscount(string userId)
    {
        try
        {
            if (!await _membershipService.RevokeMemberDiscountAsync(userId))
            {
                return NotFound(new { message = $"No discount override found for user {userId}" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking discount for {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error revoking discount" });
        }
    }

    /// <summary>
    /// Get user's active membership
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User membership details or null</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserMembershipDto>> GetUserMembership(string userId)
    {
        try
        {
            var membership = await _membershipService.GetMembershipByUserIdAsync(userId);
            if (membership == null)
            {
                return NotFound(new { message = $"No active membership found for user {userId}" });
            }
            return Ok(membership);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching membership for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching membership" });
        }
    }

    /// <summary>
    /// Create new membership for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="planName">Membership plan name</param>
    /// <returns>Created membership</returns>
    [HttpPost("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserMembershipDto>> CreateMembership(string userId, [FromQuery] string planName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(planName))
            {
                return BadRequest(new { message = "Plan name is required" });
            }

            var membership = await _membershipService.CreateMembershipAsync(userId, planName);
            return CreatedAtAction(nameof(GetUserMembership), new { userId = membership.UserId }, membership);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating membership for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating membership" });
        }
    }

    /// <summary>
    /// Cancel user membership
    /// </summary>
    /// <param name="membershipId">Membership ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{membershipId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelMembership(int membershipId)
    {
        try
        {
            var cancelled = await _membershipService.CancelMembershipAsync(membershipId);
            if (!cancelled)
            {
                return NotFound(new { message = $"Membership with id {membershipId} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling membership with id {MembershipId}", membershipId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error cancelling membership" });
        }
    }
}
