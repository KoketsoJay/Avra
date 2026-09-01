namespace WellnessService.Models;

/// <summary>
/// Represents a wellness plan
/// </summary>
public class WellnessPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Level { get; set; } = "Beginner"; // Beginner, Intermediate, Advanced
    public int DurationDays { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<UserProgress> UserProgresses { get; set; } = new List<UserProgress>();
}

/// <summary>
/// Represents user progress on a wellness plan
/// </summary>
public class UserProgress
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public int CompletedDays { get; set; } = 0;
    public string Status { get; set; } = "In Progress"; // In Progress, Completed, Paused, Cancelled
    public float ProgressPercentage { get; set; } = 0;
    public int CaloriesBurned { get; set; } = 0;
    public decimal Weight { get; set; } = 0;
    public string? Notes { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompletionDate { get; set; }
    public DateTime? LastActivityDate { get; set; }

    public WellnessPlan? Plan { get; set; }
}

/// <summary>
/// DTO for creating wellness plan
/// </summary>
public class CreateWellnessPlanDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Level { get; set; } = "Beginner";
    public int DurationDays { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
}

/// <summary>
/// DTO for wellness plan response
/// </summary>
public class WellnessPlanDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO for enrolling in wellness plan
/// </summary>
public class EnrollInPlanDto
{
    public string UserId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
}

/// <summary>
/// DTO for user progress response
/// </summary>
public class UserProgressDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public int CompletedDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public float ProgressPercentage { get; set; }
    public int CaloriesBurned { get; set; }
    public decimal Weight { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
}

/// <summary>
/// DTO for updating user progress
/// </summary>
public class UpdateProgressDto
{
    public int CompletedDays { get; set; }
    public int CaloriesBurned { get; set; }
    public decimal Weight { get; set; }
    public string? Notes { get; set; }
}
