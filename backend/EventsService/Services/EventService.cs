using Microsoft.EntityFrameworkCore;
using Avra.ServiceDefaults.Storage;
using EventsService.Data;
using EventsService.Models;
using QRCoder;

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
    Task<EventTicketPurchaseDto> PurchaseTicketsAsync(string eventId, PurchaseEventTicketsDto purchaseDto);
    Task<EventRegistrationDto?> GetRegistrationByIdAsync(string registrationId);
    Task<IReadOnlyList<UserEventTicketDto>> GetUserTicketsAsync(string userId);
    Task<EventTicketValidationResultDto> ValidateTicketAsync(ValidateEventTicketDto validateDto);
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
    private readonly IImageStorage _imageStorage;
    private readonly ILogger<EventService> _logger;

    public EventService(EventsDbContext context, IImageStorage imageStorage, ILogger<EventService> logger)
    {
        _context = context;
        _imageStorage = imageStorage;
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
            ImageUrl = _imageStorage.ToStorageReference(createDto.ImageUrl),
            IsFeatured = createDto.IsFeatured,
            IsMembersOnly = createDto.IsMembersOnly,
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
        eventEntity.ImageUrl = _imageStorage.ToStorageReference(updateDto.ImageUrl);
        eventEntity.IsFeatured = updateDto.IsFeatured;
        eventEntity.IsMembersOnly = updateDto.IsMembersOnly;
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

        // Hard delete: tickets restrict event deletion, so remove them first; registrations cascade.
        var tickets = await _context.EventTickets
            .Where(t => t.EventId == eventId)
            .ToListAsync();
        _context.EventTickets.RemoveRange(tickets);

        _context.Events.Remove(eventEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Event deleted successfully with id: {EventId}", eventId);

        return true;
    }

    private EventDto MapToDto(Event eventEntity)
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
            ImageUrl = _imageStorage.ToDisplayUrl(eventEntity.ImageUrl),
            Status = eventEntity.Status,
            IsFeatured = eventEntity.IsFeatured,
            IsMembersOnly = eventEntity.IsMembersOnly
        };
    }
}

/// <summary>
/// Implementation of IRegistrationService
/// </summary>
public class RegistrationService : IRegistrationService
{
    private readonly EventsDbContext _context;
    private readonly IPaymentGatewayClient _paymentGateway;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(EventsDbContext context, IPaymentGatewayClient paymentGateway, ILogger<RegistrationService> logger)
    {
        _context = context;
        _paymentGateway = paymentGateway;
        _logger = logger;
    }

    public async Task<EventRegistrationDto> RegisterForEventAsync(string eventId, RegisterForEventDto registerDto)
    {
        _logger.LogInformation("Registering user {UserId} for event {EventId}", registerDto.UserId, eventId);

        if (registerDto.NumberOfAttendees <= 0)
        {
            throw new InvalidOperationException("Number of attendees must be at least 1");
        }

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

        if (eventEntity.IsMembersOnly && !registerDto.IsMember)
        {
            throw new InvalidOperationException("This event is available to members only.");
        }

        if (eventEntity.Price > 0)
        {
            throw new InvalidOperationException("This is a paid event. Use the ticket purchase endpoint.");
        }

        var registration = CreateRegistration(eventId, registerDto.UserId, registerDto.UserName, registerDto.UserEmail, registerDto.NumberOfAttendees, 0m);

        _context.EventRegistrations.Add(registration);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User registered successfully for event with registration id: {RegistrationId}", registration.Id);

        return MapToDto(registration);
    }

    public async Task<EventTicketPurchaseDto> PurchaseTicketsAsync(string eventId, PurchaseEventTicketsDto purchaseDto)
    {
        ArgumentNullException.ThrowIfNull(purchaseDto);

        _logger.LogInformation("Purchasing {Count} ticket(s) for user {UserId} on event {EventId}", purchaseDto.NumberOfTickets, purchaseDto.UserId, eventId);

        if (purchaseDto.NumberOfTickets <= 0)
        {
            throw new InvalidOperationException("Number of tickets must be at least 1");
        }

        var eventEntity = await _context.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (eventEntity == null)
        {
            throw new KeyNotFoundException($"Event with id {eventId} not found");
        }

        if (eventEntity.IsMembersOnly && !purchaseDto.IsMember)
        {
            throw new InvalidOperationException("This event is available to members only.");
        }

        if (eventEntity.Price <= 0)
        {
            throw new InvalidOperationException("This event is free. Use RSVP registration instead.");
        }

        if (string.IsNullOrWhiteSpace(purchaseDto.PaymentMethod))
        {
            throw new InvalidOperationException("Payment method is required.");
        }

        if (string.IsNullOrWhiteSpace(purchaseDto.CardLast4) || purchaseDto.CardLast4.Length != 4 || !purchaseDto.CardLast4.All(char.IsDigit))
        {
            throw new InvalidOperationException("Card last 4 digits are required.");
        }

        var currentAttendees = eventEntity.Registrations
            .Where(r => r.Status == RegistrationStatus.Confirmed)
            .Sum(r => r.NumberOfAttendees);

        if (currentAttendees + purchaseDto.NumberOfTickets > eventEntity.MaxAttendees)
        {
            throw new InvalidOperationException("Event is fully booked");
        }

        var total = eventEntity.Price * purchaseDto.NumberOfTickets;

        var payment = await _paymentGateway.ChargeAsync(new PaymentChargeRequest
        {
            Amount = total,
            Currency = "ZAR",
            PaymentMethod = purchaseDto.PaymentMethod,
            CardLast4 = purchaseDto.CardLast4,
            Reference = $"event:{eventEntity.Id}:user:{purchaseDto.UserId}:tickets:{purchaseDto.NumberOfTickets}",
            Description = $"{eventEntity.Title} x{purchaseDto.NumberOfTickets}",
        });

        if (!payment.Approved)
        {
            throw new InvalidOperationException(payment.Message);
        }

        var registration = CreateRegistration(eventId, purchaseDto.UserId, purchaseDto.UserName, purchaseDto.UserEmail, purchaseDto.NumberOfTickets, total);

        var issuedAt = DateTime.UtcNow;
        var tickets = Enumerable.Range(0, purchaseDto.NumberOfTickets)
            .Select(_ => CreateTicket(registration, eventEntity, issuedAt))
            .ToList();

        _context.EventRegistrations.Add(registration);
        _context.EventTickets.AddRange(tickets);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ticket purchase completed with registration {RegistrationId}", registration.Id);

        return new EventTicketPurchaseDto
        {
            EventId = eventEntity.Id,
            EventTitle = eventEntity.Title,
            UserId = purchaseDto.UserId,
            NumberOfTickets = purchaseDto.NumberOfTickets,
            AmountPaid = total,
            PaymentMethod = purchaseDto.PaymentMethod,
            PaymentStatus = payment.Status,
            Registration = MapToDto(registration),
            Tickets = tickets.Select(MapToDto).ToList(),
        };
    }

    public async Task<EventRegistrationDto?> GetRegistrationByIdAsync(string registrationId)
    {
        _logger.LogInformation("Fetching registration with id: {RegistrationId}", registrationId);

        var registration = await _context.EventRegistrations.FindAsync(registrationId);
        return registration == null ? null : MapToDto(registration);
    }

    public async Task<IReadOnlyList<UserEventTicketDto>> GetUserTicketsAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return [];
        }

        var tickets = await _context.EventTickets
            .Include(t => t.Event)
            .Include(t => t.Registration)
            .Where(t => t.UserId == userId && t.Registration != null && t.Registration.Status != RegistrationStatus.Cancelled)
            .OrderByDescending(t => t.IssuedAt)
            .ToListAsync();

        return tickets.Select(MapToUserTicketDto).ToList();
    }

    public async Task<EventTicketValidationResultDto> ValidateTicketAsync(ValidateEventTicketDto validateDto)
    {
        ArgumentNullException.ThrowIfNull(validateDto);

        var ticketNumber = validateDto.TicketNumber.Trim();
        if (string.IsNullOrWhiteSpace(ticketNumber))
        {
            return new EventTicketValidationResultDto
            {
                IsValid = false,
                Status = "InvalidInput",
                Message = "Ticket number is required.",
            };
        }

        var ticket = await _context.EventTickets
            .Include(t => t.Event)
            .Include(t => t.Registration)
            .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber);

        if (ticket == null)
        {
            return new EventTicketValidationResultDto
            {
                IsValid = false,
                Status = "NotFound",
                Message = "Ticket not found.",
            };
        }

        if (!string.IsNullOrWhiteSpace(validateDto.EventId) && !string.Equals(ticket.EventId, validateDto.EventId, StringComparison.Ordinal))
        {
            return new EventTicketValidationResultDto
            {
                IsValid = false,
                Status = "WrongEvent",
                Message = "Ticket belongs to a different event.",
                Ticket = MapToUserTicketDto(ticket),
            };
        }

        if (ticket.IsCancelled || ticket.Registration?.Status == RegistrationStatus.Cancelled)
        {
            return new EventTicketValidationResultDto
            {
                IsValid = false,
                Status = "Cancelled",
                Message = "Ticket has been cancelled.",
                Ticket = MapToUserTicketDto(ticket),
            };
        }

        if (ticket.Event?.Status == EventStatus.Cancelled)
        {
            return new EventTicketValidationResultDto
            {
                IsValid = false,
                Status = "EventCancelled",
                Message = "This event has been cancelled.",
                Ticket = MapToUserTicketDto(ticket),
            };
        }

        if (ticket.Event != null && DateTime.UtcNow > ticket.Event.EndDate.AddHours(8))
        {
            return new EventTicketValidationResultDto
            {
                IsValid = false,
                Status = "Expired",
                Message = "Ticket has expired.",
                Ticket = MapToUserTicketDto(ticket),
            };
        }

        return new EventTicketValidationResultDto
        {
            IsValid = true,
            Status = "Valid",
            Message = "Ticket is valid for entry.",
            Ticket = MapToUserTicketDto(ticket),
        };
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

        var registration = await _context.EventRegistrations
            .Include(r => r.Tickets)
            .FirstOrDefaultAsync(r => r.Id == registrationId);
        if (registration == null)
        {
            return false;
        }

        registration.Status = RegistrationStatus.Cancelled;
        registration.CancelledAt = DateTime.UtcNow;
        foreach (var ticket in registration.Tickets)
        {
            ticket.IsCancelled = true;
        }
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

    private static EventRegistration CreateRegistration(
        string eventId,
        string userId,
        string userName,
        string userEmail,
        int attendeeCount,
        decimal amountPaid) =>
        new()
        {
            EventId = eventId,
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            NumberOfAttendees = attendeeCount,
            Status = RegistrationStatus.Confirmed,
            AmountPaid = amountPaid,
            RegisteredAt = DateTime.UtcNow,
        };

    private static EventTicket CreateTicket(EventRegistration registration, Event eventEntity, DateTime issuedAt)
    {
        var ticketNumber = BuildTicketNumber();
        var payload = $"avra:event={eventEntity.Id};ticket={ticketNumber};registration={registration.Id};user={registration.UserId};issued={issuedAt:O}";
        return new EventTicket
        {
            RegistrationId = registration.Id,
            EventId = eventEntity.Id,
            UserId = registration.UserId,
            TicketNumber = ticketNumber,
            QrCodePayload = payload,
            QrCodePngBase64 = CreateQrPngBase64(payload),
            IssuedAt = issuedAt,
        };
    }

    private static string BuildTicketNumber() =>
        $"AVRA-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

    private static string CreateQrPngBase64(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(qrData);
        var pngBytes = pngQr.GetGraphic(16);
        return Convert.ToBase64String(pngBytes);
    }

    private static EventTicketDto MapToDto(EventTicket ticket)
    {
        return new EventTicketDto
        {
            Id = ticket.Id,
            RegistrationId = ticket.RegistrationId,
            EventId = ticket.EventId,
            UserId = ticket.UserId,
            TicketNumber = ticket.TicketNumber,
            QrCodePayload = ticket.QrCodePayload,
            QrCodePngBase64 = ticket.QrCodePngBase64,
            IssuedAt = ticket.IssuedAt,
        };
    }

    private static UserEventTicketDto MapToUserTicketDto(EventTicket ticket)
    {
        return new UserEventTicketDto
        {
            Id = ticket.Id,
            RegistrationId = ticket.RegistrationId,
            EventId = ticket.EventId,
            EventTitle = ticket.Event?.Title ?? string.Empty,
            EventLocation = ticket.Event?.Location ?? string.Empty,
            EventStartDate = ticket.Event?.StartDate ?? DateTime.MinValue,
            EventEndDate = ticket.Event?.EndDate ?? DateTime.MinValue,
            TicketNumber = ticket.TicketNumber,
            QrCodePayload = ticket.QrCodePayload,
            QrCodePngBase64 = ticket.QrCodePngBase64,
            IssuedAt = ticket.IssuedAt,
            IsCancelled = ticket.IsCancelled,
        };
    }
}
