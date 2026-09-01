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

    /// <summary>When false the event is saved as a draft and stays off the storefront.</summary>
    public bool PublishImmediately { get; set; } = true;
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
