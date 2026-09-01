using Microsoft.EntityFrameworkCore;
using WellnessService.Data;
using WellnessService.Models;

namespace WellnessService.Services;

/// <summary>
/// Service for managing wellness plans
/// </summary>
public interface IWellnessPlanService
{
    Task<IEnumerable<WellnessPlanDto>> GetAllPlansAsync();
    Task<IEnumerable<WellnessPlanDto>> GetPlansByCategoryAsync(string category);
    Task<IEnumerable<WellnessPlanDto>> GetPlansByLevelAsync(string level);
    Task<WellnessPlanDto?> GetPlanByIdAsync(string planId);
    Task<WellnessPlanDto> CreatePlanAsync(CreateWellnessPlanDto createDto);
    Task<WellnessPlanDto> UpdatePlanAsync(string planId, CreateWellnessPlanDto updateDto);
    Task<bool> DeletePlanAsync(string planId);
}

/// <summary>
/// Service for managing user progress
/// </summary>
public interface IProgressService
{
    Task<UserProgressDto> EnrollInPlanAsync(string userId, string planId);
    Task<UserProgressDto?> GetProgressByIdAsync(string progressId);
    Task<UserProgressDto?> GetUserPlanProgressAsync(string userId, string planId);
    Task<IEnumerable<UserProgressDto>> GetUserProgressesAsync(string userId);
    Task<UserProgressDto> UpdateProgressAsync(string progressId, UpdateProgressDto updateDto);
    Task<UserProgressDto> UpdatePlanStatusAsync(string progressId, string newStatus);
    Task<bool> CompleteProgressAsync(string progressId);
}

/// <summary>
/// Implementation of IWellnessPlanService
/// </summary>
public class WellnessPlanService : IWellnessPlanService
{
    private readonly WellnessDbContext _context;
    private readonly ILogger<WellnessPlanService> _logger;

    public WellnessPlanService(WellnessDbContext context, ILogger<WellnessPlanService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<WellnessPlanDto>> GetAllPlansAsync()
    {
        _logger.LogInformation("Fetching all active wellness plans");

        var plans = await _context.WellnessPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category)
            .ToListAsync();

        return plans.Select(MapToDto);
    }

    public async Task<IEnumerable<WellnessPlanDto>> GetPlansByCategoryAsync(string category)
    {
        _logger.LogInformation("Fetching wellness plans by category: {Category}", category);

        var plans = await _context.WellnessPlans
            .Where(p => p.Category.Contains(category) && p.IsActive)
            .OrderBy(p => p.Level)
            .ToListAsync();

        return plans.Select(MapToDto);
    }

    public async Task<IEnumerable<WellnessPlanDto>> GetPlansByLevelAsync(string level)
    {
        _logger.LogInformation("Fetching wellness plans by level: {Level}", level);

        var plans = await _context.WellnessPlans
            .Where(p => p.Level == level && p.IsActive)
            .OrderBy(p => p.Price)
            .ToListAsync();

        return plans.Select(MapToDto);
    }

    public async Task<WellnessPlanDto?> GetPlanByIdAsync(string planId)
    {
        _logger.LogInformation("Fetching wellness plan with id: {PlanId}", planId);

        var plan = await _context.WellnessPlans.FindAsync(planId);
        return plan == null ? null : MapToDto(plan);
    }

    public async Task<WellnessPlanDto> CreatePlanAsync(CreateWellnessPlanDto createDto)
    {
        _logger.LogInformation("Creating new wellness plan: {Name}", createDto.Name);

        var plan = new WellnessPlan
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Category = createDto.Category,
            Level = createDto.Level,
            DurationDays = createDto.DurationDays,
            ImageUrl = createDto.ImageUrl,
            Price = createDto.Price,
            CreatedAt = DateTime.UtcNow
        };

        _context.WellnessPlans.Add(plan);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Wellness plan created successfully with id: {PlanId}", plan.Id);

        return MapToDto(plan);
    }

    public async Task<WellnessPlanDto> UpdatePlanAsync(string planId, CreateWellnessPlanDto updateDto)
    {
        _logger.LogInformation("Updating wellness plan with id: {PlanId}", planId);

        var plan = await _context.WellnessPlans.FindAsync(planId);
        if (plan == null)
        {
            throw new KeyNotFoundException($"Wellness plan with id {planId} not found");
        }

        plan.Name = updateDto.Name;
        plan.Description = updateDto.Description;
        plan.Category = updateDto.Category;
        plan.Level = updateDto.Level;
        plan.DurationDays = updateDto.DurationDays;
        plan.ImageUrl = updateDto.ImageUrl;
        plan.Price = updateDto.Price;
        plan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Wellness plan updated successfully with id: {PlanId}", planId);

        return MapToDto(plan);
    }

    public async Task<bool> DeletePlanAsync(string planId)
    {
        _logger.LogInformation("Deleting wellness plan with id: {PlanId}", planId);

        var plan = await _context.WellnessPlans.FindAsync(planId);
        if (plan == null)
        {
            return false;
        }

        plan.IsActive = false;
        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Wellness plan deleted successfully with id: {PlanId}", planId);

        return true;
    }

    private static WellnessPlanDto MapToDto(WellnessPlan plan)
    {
        return new WellnessPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Category = plan.Category,
            Level = plan.Level,
            DurationDays = plan.DurationDays,
            ImageUrl = plan.ImageUrl,
            Price = plan.Price,
            IsActive = plan.IsActive
        };
    }
}

/// <summary>
/// Implementation of IProgressService
/// </summary>
public class ProgressService : IProgressService
{
    private readonly WellnessDbContext _context;
    private readonly ILogger<ProgressService> _logger;

    public ProgressService(WellnessDbContext context, ILogger<ProgressService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserProgressDto> EnrollInPlanAsync(string userId, string planId)
    {
        _logger.LogInformation("Enrolling user {UserId} in plan {PlanId}", userId, planId);

        var plan = await _context.WellnessPlans.FindAsync(planId);
        if (plan == null)
        {
            throw new KeyNotFoundException($"Wellness plan with id {planId} not found");
        }

        var progress = new UserProgress
        {
            UserId = userId,
            PlanId = planId,
            Status = "In Progress",
            ProgressPercentage = 0,
            StartDate = DateTime.UtcNow,
            LastActivityDate = DateTime.UtcNow
        };

        _context.UserProgresses.Add(progress);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User enrolled successfully in plan with progress id: {ProgressId}", progress.Id);

        return MapToDto(progress, plan.Name);
    }

    public async Task<UserProgressDto?> GetProgressByIdAsync(string progressId)
    {
        _logger.LogInformation("Fetching progress with id: {ProgressId}", progressId);

        var progress = await _context.UserProgresses
            .Include(p => p.Plan)
            .FirstOrDefaultAsync(p => p.Id == progressId);

        return progress == null ? null : MapToDto(progress, progress.Plan?.Name ?? "Unknown");
    }

    public async Task<UserProgressDto?> GetUserPlanProgressAsync(string userId, string planId)
    {
        _logger.LogInformation("Fetching progress for user {UserId} in plan {PlanId}", userId, planId);

        var progress = await _context.UserProgresses
            .Include(p => p.Plan)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.PlanId == planId);

        return progress == null ? null : MapToDto(progress, progress.Plan?.Name ?? "Unknown");
    }

    public async Task<IEnumerable<UserProgressDto>> GetUserProgressesAsync(string userId)
    {
        _logger.LogInformation("Fetching all progresses for user: {UserId}", userId);

        var progresses = await _context.UserProgresses
            .Include(p => p.Plan)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();

        return progresses.Select(p => MapToDto(p, p.Plan?.Name ?? "Unknown"));
    }

    public async Task<UserProgressDto> UpdateProgressAsync(string progressId, UpdateProgressDto updateDto)
    {
        _logger.LogInformation("Updating progress with id: {ProgressId}", progressId);

        var progress = await _context.UserProgresses
            .Include(p => p.Plan)
            .FirstOrDefaultAsync(p => p.Id == progressId);

        if (progress == null)
        {
            throw new KeyNotFoundException($"Progress with id {progressId} not found");
        }

        progress.CompletedDays = updateDto.CompletedDays;
        progress.CaloriesBurned = updateDto.CaloriesBurned;
        progress.Weight = updateDto.Weight;
        progress.Notes = updateDto.Notes;
        progress.LastActivityDate = DateTime.UtcNow;

        // Calculate progress percentage
        if (progress.Plan != null)
        {
            progress.ProgressPercentage = (float)progress.CompletedDays / progress.Plan.DurationDays * 100;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Progress updated successfully with id: {ProgressId}", progressId);

        return MapToDto(progress, progress.Plan?.Name ?? "Unknown");
    }

    public async Task<UserProgressDto> UpdatePlanStatusAsync(string progressId, string newStatus)
    {
        _logger.LogInformation("Updating plan status for progress id: {ProgressId}, Status: {Status}", progressId, newStatus);

        var progress = await _context.UserProgresses
            .Include(p => p.Plan)
            .FirstOrDefaultAsync(p => p.Id == progressId);

        if (progress == null)
        {
            throw new KeyNotFoundException($"Progress with id {progressId} not found");
        }

        progress.Status = newStatus;
        progress.LastActivityDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Plan status updated successfully for progress id: {ProgressId}", progressId);

        return MapToDto(progress, progress.Plan?.Name ?? "Unknown");
    }

    public async Task<bool> CompleteProgressAsync(string progressId)
    {
        _logger.LogInformation("Completing progress with id: {ProgressId}", progressId);

        var progress = await _context.UserProgresses.FindAsync(progressId);
        if (progress == null)
        {
            return false;
        }

        progress.Status = "Completed";
        progress.ProgressPercentage = 100;
        progress.CompletionDate = DateTime.UtcNow;
        progress.LastActivityDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Progress completed successfully with id: {ProgressId}", progressId);

        return true;
    }

    private static UserProgressDto MapToDto(UserProgress progress, string planName)
    {
        return new UserProgressDto
        {
            Id = progress.Id,
            UserId = progress.UserId,
            PlanId = progress.PlanId,
            PlanName = planName,
            CompletedDays = progress.CompletedDays,
            Status = progress.Status,
            ProgressPercentage = progress.ProgressPercentage,
            CaloriesBurned = progress.CaloriesBurned,
            Weight = progress.Weight,
            StartDate = progress.StartDate,
            CompletionDate = progress.CompletionDate
        };
    }
}
