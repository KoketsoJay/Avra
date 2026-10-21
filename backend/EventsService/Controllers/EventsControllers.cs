using Avra.ServiceDefaults.Security;
using Avra.ServiceDefaults.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventsService.Models;
using EventsService.Services;

namespace EventsService.Controllers;

/// <summary>
/// API controller for event operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IImageStorage _imageStorage;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventService eventService, IImageStorage imageStorage, ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _imageStorage = imageStorage;
        _logger = logger;
    }

    /// <summary>
    /// Get all active events
    /// </summary>
    /// <returns>List of events</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetAllEvents()
    {
        try
        {
            var events = await _eventService.GetAllEventsAsync();
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all events");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching events" });
        }
    }

    /// <summary>
    /// Get featured events
    /// </summary>
    /// <returns>List of featured events</returns>
    [HttpGet("featured")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetFeaturedEvents()
    {
        try
        {
            var events = await _eventService.GetFeaturedEventsAsync();
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching featured events");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching featured events" });
        }
    }

    /// <summary>
    /// Get every event including drafts and cancelled ones. Administrators only.
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetAllEventsForAdmin()
    {
        try
        {
            return Ok(await _eventService.GetAllEventsForAdminAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching events for the administrator view");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching events" });
        }
    }

    /// <summary>
    /// Get event by ID
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <returns>Event details</returns>
    [HttpGet("{eventId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetEvent(string eventId)
    {
        try
        {
            var eventEntity = await _eventService.GetEventByIdAsync(eventId);
            if (eventEntity == null)
            {
                return NotFound(new { message = $"Event with id {eventId} not found" });
            }
            return Ok(eventEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event with id {EventId}", eventId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching event" });
        }
    }

    /// <summary>
    /// Search events by category
    /// </summary>
    /// <param name="category">Event category</param>
    /// <returns>List of events matching the category</returns>
    [HttpGet("search/category")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventDto>>> SearchByCategory([FromQuery] string category)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return BadRequest(new { message = "Category is required" });
            }

            var events = await _eventService.SearchEventsByCategoryAsync(category);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching events by category: {Category}", category);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error searching events" });
        }
    }

    /// <summary>
    /// Upload an event image to blob storage and return its stored reference.
    /// </summary>
    [HttpPost("images")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImageUploadResultDto>> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        if (!_imageStorage.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not configured." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var reference = await _imageStorage.UploadAsync(stream, file.ContentType, cancellationToken);
            return Ok(new ImageUploadResultDto { Reference = reference, Url = _imageStorage.ToDisplayUrl(reference) });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading event image");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error uploading image" });
        }
    }

    /// <summary>
    /// Create new event
    /// </summary>
    /// <param name="createDto">Event creation data</param>
    /// <returns>Created event</returns>
    [HttpPost]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventDto>> CreateEvent(CreateEventDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var eventEntity = await _eventService.CreateEventAsync(createDto);
            return CreatedAtAction(nameof(GetEvent), new { eventId = eventEntity.Id }, eventEntity);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Event creation rejected");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating event" });
        }
    }

    /// <summary>
    /// Update event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="updateDto">Event update data</param>
    /// <returns>Updated event</returns>
    [HttpPut("{eventId}")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> UpdateEvent(string eventId, CreateEventDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var eventEntity = await _eventService.UpdateEventAsync(eventId, updateDto);
            return Ok(eventEntity);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Event not found with id {EventId}", eventId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event with id {EventId}", eventId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating event" });
        }
    }

    /// <summary>
    /// Publish a draft event so it appears on the storefront. Administrators only.
    /// </summary>
    /// <param name="eventId">Event ID</param>
    [HttpPut("{eventId}/publish")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<EventDto>> PublishEvent(string eventId) =>
        ChangeStatusAsync(eventId, EventStatus.Active);

    /// <summary>
    /// Return a published event to draft so it leaves the storefront. Administrators only.
    /// </summary>
    /// <param name="eventId">Event ID</param>
    [HttpPut("{eventId}/unpublish")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<EventDto>> UnpublishEvent(string eventId) =>
        ChangeStatusAsync(eventId, EventStatus.Draft);

    /// <summary>
    /// Delete event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{eventId}")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(string eventId)
    {
        try
        {
            var deleted = await _eventService.DeleteEventAsync(eventId);
            if (!deleted)
            {
                return NotFound(new { message = $"Event with id {eventId} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event with id {EventId}", eventId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error deleting event" });
        }
    }

    private async Task<ActionResult<EventDto>> ChangeStatusAsync(string eventId, EventStatus status)
    {
        try
        {
            return Ok(await _eventService.SetEventStatusAsync(eventId, status));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Event not found with id {EventId}", eventId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing status of event {EventId}", eventId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating event status" });
        }
    }
}

/// <summary>
/// API controller for event registration operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RegistrationsController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly ICurrentUser _currentUser;
    private readonly IMembershipAccessClient _membershipAccessClient;
    private readonly ILogger<RegistrationsController> _logger;

    public RegistrationsController(
        IRegistrationService registrationService,
        ICurrentUser currentUser,
        IMembershipAccessClient membershipAccessClient,
        ILogger<RegistrationsController> logger)
    {
        _registrationService = registrationService;
        _currentUser = currentUser;
        _membershipAccessClient = membershipAccessClient;
        _logger = logger;
    }

    /// <summary>
    /// Register user for event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="registerDto">Registration data</param>
    /// <returns>Registration confirmation</returns>
    [HttpPost("events/{eventId}/register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventRegistrationDto>> RegisterForEvent(string eventId, RegisterForEventDto registerDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            registerDto.IsMember = await PopulateMembershipContextAsync(registerDto.UserId);
            if (!string.IsNullOrWhiteSpace(_currentUser.UserId))
            {
                registerDto.UserId = _currentUser.UserId!;
            }

            var registration = await _registrationService.RegisterForEventAsync(eventId, registerDto);
            return CreatedAtAction(nameof(GetRegistration), new { registrationId = registration.Id }, registration);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Event not found with id {EventId}", eventId);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot register for event {EventId}", eventId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering for event {EventId}", eventId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error registering for event" });
        }
    }

    /// <summary>
    /// Purchase paid event tickets and receive issued QR tickets.
    /// </summary>
    [HttpPost("events/{eventId}/purchase")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventTicketPurchaseDto>> PurchaseTickets(string eventId, PurchaseEventTicketsDto purchaseDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            purchaseDto.IsMember = await PopulateMembershipContextAsync(purchaseDto.UserId);
            if (!string.IsNullOrWhiteSpace(_currentUser.UserId))
            {
                purchaseDto.UserId = _currentUser.UserId!;
            }

            var purchase = await _registrationService.PurchaseTicketsAsync(eventId, purchaseDto);
            return CreatedAtAction(nameof(GetRegistration), new { registrationId = purchase.Registration.Id }, purchase);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Event not found with id {EventId}", eventId);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot purchase ticket(s) for event {EventId}", eventId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purchasing tickets for event {EventId}", eventId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error purchasing tickets" });
        }
    }

    /// <summary>
    /// Get registration by ID
    /// </summary>
    /// <param name="registrationId">Registration ID</param>
    /// <returns>Registration details</returns>
    [HttpGet("{registrationId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventRegistrationDto>> GetRegistration(string registrationId)
    {
        try
        {
            var registration = await _registrationService.GetRegistrationByIdAsync(registrationId);
            if (registration == null)
            {
                return NotFound(new { message = $"Registration with id {registrationId} not found" });
            }
            return Ok(registration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching registration with id {RegistrationId}", registrationId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching registration" });
        }
    }

    /// <summary>
    /// Get all registrations for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of user registrations</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventRegistrationDto>>> GetUserRegistrations(string userId)
    {
        try
        {
            var registrations = await _registrationService.GetUserRegistrationsAsync(userId);
            return Ok(registrations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching registrations for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching registrations" });
        }
    }

    /// <summary>
    /// Ticket wallet for a user.
    /// </summary>
    [HttpGet("tickets/user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserEventTicketDto>>> GetUserTickets(string userId)
    {
        try
        {
            return Ok(await _registrationService.GetUserTicketsAsync(userId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tickets for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching tickets" });
        }
    }

    /// <summary>
    /// Validate an issued QR ticket for check-in.
    /// </summary>
    [HttpPost("tickets/validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<EventTicketValidationResultDto>> ValidateTicket([FromBody] ValidateEventTicketDto validateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Ok(await _registrationService.ValidateTicketAsync(validateDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating ticket {TicketNumber}", validateDto.TicketNumber);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error validating ticket" });
        }
    }

    /// <summary>
    /// Cancel registration
    /// </summary>
    /// <param name="registrationId">Registration ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{registrationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelRegistration(string registrationId)
    {
        try
        {
            var cancelled = await _registrationService.CancelRegistrationAsync(registrationId);
            if (!cancelled)
            {
                return NotFound(new { message = $"Registration with id {registrationId} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling registration with id {RegistrationId}", registrationId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error cancelling registration" });
        }
    }

    private async Task<bool> PopulateMembershipContextAsync(string fallbackUserId)
    {
        var bearerHeader = HttpContext.Request.Headers.Authorization.ToString();
        var accessToken = bearerHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? bearerHeader[7..].Trim()
            : string.Empty;

        var effectiveUserId = string.IsNullOrWhiteSpace(_currentUser.UserId) ? fallbackUserId : _currentUser.UserId!;
        if (string.IsNullOrWhiteSpace(effectiveUserId))
        {
            return false;
        }

        return await _membershipAccessClient.IsCurrentUserMemberAsync(
            accessToken,
            effectiveUserId,
            HttpContext.RequestAborted);
    }
}
