using Microsoft.EntityFrameworkCore;
using EventsService.Data;
using EventsService.Models;

namespace EventsService.Services;

/// <summary>
/// Service for managing events
/// </summary>
public interface IEventService
{
    Task<IEnumerable<EventDto>> GetAllEventsAsync();
    Task<IEnumerable<EventDto>> GetFeaturedEventsAsync();

    /// <summary>Every event including drafts and cancelled ones. Administrator view.</summary>
    Task<IEnumerable<EventDto>> GetAllEventsForAdminAsync();

    Task<EventDto?> GetEventByIdAsync(string eventId);
    Task<IEnumerable<EventDto>> SearchEventsByCategoryAsync(string category);
    Task<EventDto> CreateEventAsync(CreateEventDto createDto);
    Task<EventDto> UpdateEventAsync(string eventId, CreateEventDto updateDto);

    /// <summary>Moves an event between draft and published states.</summary>
    Task<EventDto> SetEventStatusAsync(string eventId, EventStatus status);

    Task<bool> DeleteEventAsync(string eventId);
}

/// <summary>
/// Service for managing event registrations
/// </summary>
public interface IRegistrationService
{
    Task<EventRegistrationDto> RegisterForEventAsync(string eventId, RegisterForEventDto registerDto);
    Task<EventRegistrationDto?> GetRegistrationByIdAsync(string registrationId);
    Task<IEnumerable<EventRegistrationDto>> GetUserRegistrationsAsync(string userId);
    Task<IEnumerable<EventRegistrationDto>> GetEventRegistrationsAsync(string eventId);
    Task<bool> CancelRegistrationAsync(string registrationId);
}

/// <summary>
/// Implementation of IEventService
/// </summary>
public class EventService : IEventService
{
    private readonly EventsDbContext _context;
    private readonly ILogger<EventService> _logger;

    public EventService(EventsDbContext context, ILogger<EventService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<EventDto>> GetAllEventsAsync()
    {
        _logger.LogInformation("Fetching all active events");

        var events = await _context.Events
            .Where(e => e.Status == EventStatus.Active || e.Status == EventStatus.Upcoming)
            .OrderByDescending(e => e.IsFeatured)
            .ThenBy(e => e.StartDate)
            .ToListAsync();

        return events.Select(MapToDto);
    }

    public async Task<IEnumerable<EventDto>> GetFeaturedEventsAsync()
    {
        _logger.LogInformation("Fetching featured events");

        var events = await _context.Events
            .Where(e => e.IsFeatured && (e.Status == EventStatus.Active || e.Status == EventStatus.Upcoming))
            .OrderBy(e => e.StartDate)
            .Take(5)
            .ToListAsync();

        return events.Select(MapToDto);
    }

    public async Task<EventDto?> GetEventByIdAsync(string eventId)
    {
        _logger.LogInformation("Fetching event with id: {EventId}", eventId);

        var eventEntity = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        return eventEntity == null ? null : MapToDto(eventEntity);
    }

    public async Task<IEnumerable<EventDto>> GetAllEventsForAdminAsync()
    {
        _logger.LogInformation("Fetching every event for the administrator view");

        var events = await _context.Events
            .Include(e => e.Registrations)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync();

        return events.Select(MapToDto);
    }

    public async Task<IEnumerable<EventDto>> SearchEventsByCategoryAsync(string category)
    {
        _logger.LogInformation("Searching events by category: {Category}", category);

        var events = await _context.Events
            .Where(e => e.Category.Contains(category) && 
                       (e.Status == EventStatus.Active || e.Status == EventStatus.Upcoming))
            .OrderBy(e => e.StartDate)
            .ToListAsync();

        return events.Select(MapToDto);
    }

    public async Task<EventDto> CreateEventAsync(CreateEventDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        _logger.LogInformation("Creating new event: {Title}", createDto.Title);

        if (createDto.EndDate <= createDto.StartDate)
        {
            throw new InvalidOperationException("The event must end after it starts");
        }

        var eventEntity = new Event
        {
            Title = createDto.Title,
            Description = createDto.Description,
            Category = createDto.Category,
            Location = createDto.Location,
            StartDate = createDto.StartDate,
            EndDate = createDto.EndDate,
            MaxAttendees = createDto.MaxAttendees,
            Price = createDto.Price,
            Organizer = createDto.Organizer,
            ImageUrl = createDto.ImageUrl,
            IsFeatured = createDto.IsFeatured,
            Status = createDto.PublishImmediately ? EventStatus.Active : EventStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Event created successfully with id: {EventId}", eventEntity.Id);

        return MapToDto(eventEntity);
    }

    public async Task<EventDto> UpdateEventAsync(string eventId, CreateEventDto updateDto)
    {
        _logger.LogInformation("Updating event with id: {EventId}", eventId);

        var eventEntity = await _context.Events.FindAsync(eventId);
        if (eventEntity == null)
        {
            throw new KeyNotFoundException($"Event with id {eventId} not found");
        }

        eventEntity.Title = updateDto.Title;
        eventEntity.Description = updateDto.Description;
        eventEntity.Category = updateDto.Category;
        eventEntity.Location = updateDto.Location;
        eventEntity.StartDate = updateDto.StartDate;
        eventEntity.EndDate = updateDto.EndDate;
        eventEntity.MaxAttendees = updateDto.MaxAttendees;
        eventEntity.Price = updateDto.Price;
        eventEntity.Organizer = updateDto.Organizer;
        eventEntity.ImageUrl = updateDto.ImageUrl;
        eventEntity.IsFeatured = updateDto.IsFeatured;
        eventEntity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Event updated successfully with id: {EventId}", eventId);

        return MapToDto(eventEntity);
    }

    public async Task<EventDto> SetEventStatusAsync(string eventId, EventStatus status)
    {
        var eventEntity = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId)
            ?? throw new KeyNotFoundException($"Event with id {eventId} not found");

        eventEntity.Status = status;
        eventEntity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Event {EventId} moved to {Status}", eventId, status);

        return MapToDto(eventEntity);
    }

    public async Task<bool> DeleteEventAsync(string eventId)
    {
        _logger.LogInformation("Deleting event with id: {EventId}", eventId);

        var eventEntity = await _context.Events.FindAsync(eventId);
        if (eventEntity == null)
        {
            return false;
        }

        eventEntity.Status = EventStatus.Cancelled;
        eventEntity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Event deleted successfully with id: {EventId}", eventId);

        return true;
    }

    private static EventDto MapToDto(Event eventEntity)
    {
        var currentAttendees = eventEntity.Registrations?
            .Where(r => r.Status == RegistrationStatus.Confirmed)
            .Sum(r => r.NumberOfAttendees) ?? 0;

        return new EventDto
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            Category = eventEntity.Category,
            Location = eventEntity.Location,
            StartDate = eventEntity.StartDate,
            EndDate = eventEntity.EndDate,
            MaxAttendees = eventEntity.MaxAttendees,
            CurrentAttendees = currentAttendees,
            Price = eventEntity.Price,
            Organizer = eventEntity.Organizer,
            ImageUrl = eventEntity.ImageUrl,
            Status = eventEntity.Status,
            IsFeatured = eventEntity.IsFeatured
        };
    }
}

/// <summary>
/// Implementation of IRegistrationService
/// </summary>
public class RegistrationService : IRegistrationService
{
    private readonly EventsDbContext _context;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(EventsDbContext context, ILogger<RegistrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EventRegistrationDto> RegisterForEventAsync(string eventId, RegisterForEventDto registerDto)
    {
        _logger.LogInformation("Registering user {UserId} for event {EventId}", registerDto.UserId, eventId);

        var eventEntity = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (eventEntity == null)
        {
            throw new KeyNotFoundException($"Event with id {eventId} not found");
        }

        var currentAttendees = eventEntity.Registrations
            .Where(r => r.Status == RegistrationStatus.Confirmed)
            .Sum(r => r.NumberOfAttendees);

        if (currentAttendees + registerDto.NumberOfAttendees > eventEntity.MaxAttendees)
        {
            throw new InvalidOperationException("Event is fully booked");
        }

        var registration = new EventRegistration
        {
            EventId = eventId,
            UserId = registerDto.UserId,
            UserName = registerDto.UserName,
            UserEmail = registerDto.UserEmail,
            NumberOfAttendees = registerDto.NumberOfAttendees,
            Status = RegistrationStatus.Confirmed,
            AmountPaid = eventEntity.Price * registerDto.NumberOfAttendees,
            RegisteredAt = DateTime.UtcNow
        };

        _context.EventRegistrations.Add(registration);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User registered successfully for event with registration id: {RegistrationId}", registration.Id);

        return MapToDto(registration);
    }

    public async Task<EventRegistrationDto?> GetRegistrationByIdAsync(string registrationId)
    {
        _logger.LogInformation("Fetching registration with id: {RegistrationId}", registrationId);

        var registration = await _context.EventRegistrations.FindAsync(registrationId);
        return registration == null ? null : MapToDto(registration);
    }

    public async Task<IEnumerable<EventRegistrationDto>> GetUserRegistrationsAsync(string userId)
    {
        _logger.LogInformation("Fetching registrations for user: {UserId}", userId);

        var registrations = await _context.EventRegistrations
            .Where(r => r.UserId == userId && r.Status != RegistrationStatus.Cancelled)
            .OrderByDescending(r => r.RegisteredAt)
            .ToListAsync();

        return registrations.Select(MapToDto);
    }

    public async Task<IEnumerable<EventRegistrationDto>> GetEventRegistrationsAsync(string eventId)
    {
        _logger.LogInformation("Fetching registrations for event: {EventId}", eventId);

        var registrations = await _context.EventRegistrations
            .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed)
            .OrderByDescending(r => r.RegisteredAt)
            .ToListAsync();

        return registrations.Select(MapToDto);
    }

    public async Task<bool> CancelRegistrationAsync(string registrationId)
    {
        _logger.LogInformation("Cancelling registration with id: {RegistrationId}", registrationId);

        var registration = await _context.EventRegistrations.FindAsync(registrationId);
        if (registration == null)
        {
            return false;
        }

        registration.Status = RegistrationStatus.Cancelled;
        registration.CancelledAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Registration cancelled successfully with id: {RegistrationId}", registrationId);

        return true;
    }

    private static EventRegistrationDto MapToDto(EventRegistration registration)
    {
        return new EventRegistrationDto
        {
            Id = registration.Id,
            EventId = registration.EventId,
            UserId = registration.UserId,
            UserName = registration.UserName,
            UserEmail = registration.UserEmail,
            NumberOfAttendees = registration.NumberOfAttendees,
            Status = registration.Status,
            AmountPaid = registration.AmountPaid,
            RegisteredAt = registration.RegisteredAt
        };
    }
}
