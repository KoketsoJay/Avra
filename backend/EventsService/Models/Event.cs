namespace EventsService.Models;

/// <summary>
/// Represents a wellness event
/// </summary>
public class Event
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxAttendees { get; set; }
    public decimal Price { get; set; }
    public string Organizer { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Active;
    public bool IsFeatured { get; set; }
    public bool IsMembersOnly { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
}

/// <summary>
/// Represents an event registration
/// </summary>
public class EventRegistration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int NumberOfAttendees { get; set; } = 1;
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Confirmed;
    public decimal AmountPaid { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }

    public Event? Event { get; set; }
    public ICollection<EventTicket> Tickets { get; set; } = new List<EventTicket>();
}

/// <summary>
/// A single issued ticket belonging to an event registration.
/// </summary>
public class EventTicket
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RegistrationId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public string QrCodePayload { get; set; } = string.Empty;
    public string QrCodePngBase64 { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public bool IsCancelled { get; set; }

    public EventRegistration? Registration { get; set; }
    public Event? Event { get; set; }
}

/// <summary>
/// Event status enumeration
/// </summary>
public enum EventStatus
{
    Active,
    Upcoming,
    OnGoing,
    Completed,
    Cancelled,
    Draft
}

/// <summary>
/// Registration status enumeration
/// </summary>
public enum RegistrationStatus
{
    Pending,
    Confirmed,
    Cancelled,
    NoShow
}

/// <summary>
/// DTO for creating/updating events
/// </summary>
public class CreateEventDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxAttendees { get; set; }
    public decimal Price { get; set; }
    public string Organizer { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsMembersOnly { get; set; }

    /// <summary>When false the event is saved as a draft and stays off the storefront.</summary>
    public bool PublishImmediately { get; set; } = true;
}

/// <summary>
/// Result of uploading an event image: the stored reference to persist and a signed preview URL.
/// </summary>
public class ImageUploadResultDto
{
    /// <summary>The stored blob reference to save against the event.</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>A short-lived signed URL for previewing the freshly uploaded image.</summary>
    public string? Url { get; set; }
}

/// <summary>
/// DTO for event response
/// </summary>
public class EventDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxAttendees { get; set; }
    public int CurrentAttendees { get; set; }
    public decimal Price { get; set; }
    public string Organizer { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public EventStatus Status { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsMembersOnly { get; set; }
}

/// <summary>
/// DTO for event registration
/// </summary>
public class RegisterForEventDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int NumberOfAttendees { get; set; } = 1;
    public bool IsMember { get; set; }
}

/// <summary>
/// DTO for paid event ticket checkout.
/// </summary>
public class PurchaseEventTicketsDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int NumberOfTickets { get; set; } = 1;
    public string PaymentMethod { get; set; } = "Card";
    public string CardLast4 { get; set; } = string.Empty;
    public bool IsMember { get; set; }
}

/// <summary>
/// DTO for event registration response
/// </summary>
public class EventRegistrationDto
{
    public string Id { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int NumberOfAttendees { get; set; }
    public RegistrationStatus Status { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime RegisteredAt { get; set; }
}

/// <summary>
/// DTO representing one issued event ticket.
/// </summary>
public class EventTicketDto
{
    public string Id { get; set; } = string.Empty;
    public string RegistrationId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TicketNumber { get; set; } = string.Empty;
    public string QrCodePayload { get; set; } = string.Empty;
    public string QrCodePngBase64 { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
}

/// <summary>
/// Checkout result for paid event tickets.
/// </summary>
public class EventTicketPurchaseDto
{
    public string EventId { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int NumberOfTickets { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public EventRegistrationDto Registration { get; set; } = new();
    public IReadOnlyList<EventTicketDto> Tickets { get; set; } = [];
}

/// <summary>
/// One issued ticket in a user's wallet.
/// </summary>
public class UserEventTicketDto
{
    public string Id { get; set; } = string.Empty;
    public string RegistrationId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string EventLocation { get; set; } = string.Empty;
    public DateTime EventStartDate { get; set; }
    public DateTime EventEndDate { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string QrCodePayload { get; set; } = string.Empty;
    public string QrCodePngBase64 { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public bool IsCancelled { get; set; }
}

/// <summary>
/// Payload used by check-in scanners to validate a ticket.
/// </summary>
public class ValidateEventTicketDto
{
    public string TicketNumber { get; set; } = string.Empty;
    public string? EventId { get; set; }
}

/// <summary>
/// Result returned by ticket validation.
/// </summary>
public class EventTicketValidationResultDto
{
    public bool IsValid { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public UserEventTicketDto? Ticket { get; set; }
}
