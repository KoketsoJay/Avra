namespace EventsService.Models;

/// <summary>
/// A marketing promotion published by an administrator. Promotions are drafted first and only
/// become visible to shoppers once published and inside their active window.
/// </summary>
public class Promotion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    /// <summary>Code shoppers enter at checkout. Optional for announcement-only promotions.</summary>
    public string? PromoCode { get; set; }

    /// <summary>Percentage taken off the qualifying items, 0-100.</summary>
    public decimal DiscountPercentage { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public PromotionStatus Status { get; set; } = PromotionStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>True when the promotion is published and today falls inside its window.</summary>
    public bool IsLive(DateTime asOf) =>
        Status == PromotionStatus.Published && StartDate <= asOf && EndDate >= asOf;
}

/// <summary>Publication state of a promotion.</summary>
public enum PromotionStatus
{
    Draft,
    Published,
    Archived,
}

/// <summary>DTO for creating and updating promotions.</summary>
public class CreatePromotionDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? PromoCode { get; set; }
    public decimal DiscountPercentage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; }
}

/// <summary>DTO returned for a promotion.</summary>
public class PromotionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? PromoCode { get; set; }
    public decimal DiscountPercentage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public PromotionStatus Status { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool IsLive { get; set; }
}
