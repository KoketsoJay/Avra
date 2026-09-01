using EventsService.Data;
using EventsService.Models;
using Microsoft.EntityFrameworkCore;

namespace EventsService.Services;

/// <summary>
/// Publishing workflow for marketing promotions.
/// </summary>
public interface IPromotionService
{
    /// <summary>Promotions currently visible to shoppers.</summary>
    Task<IEnumerable<PromotionDto>> GetLivePromotionsAsync();

    /// <summary>Every promotion, including drafts and archived ones. Administrator view.</summary>
    Task<IEnumerable<PromotionDto>> GetAllPromotionsAsync();

    Task<PromotionDto?> GetPromotionByIdAsync(string promotionId);

    Task<PromotionDto> CreatePromotionAsync(CreatePromotionDto createDto);

    Task<PromotionDto> UpdatePromotionAsync(string promotionId, CreatePromotionDto updateDto);

    Task<PromotionDto> SetStatusAsync(string promotionId, PromotionStatus status);

    Task<bool> DeletePromotionAsync(string promotionId);
}

/// <inheritdoc />
public class PromotionService(EventsDbContext context, ILogger<PromotionService> logger) : IPromotionService
{
    public async Task<IEnumerable<PromotionDto>> GetLivePromotionsAsync()
    {
        var now = DateTime.UtcNow;

        var promotions = await context.Promotions
            .Where(p => p.Status == PromotionStatus.Published && p.StartDate <= now && p.EndDate >= now)
            .OrderByDescending(p => p.IsFeatured)
            .ThenBy(p => p.EndDate)
            .ToListAsync();

        return promotions.Select(MapToDto);
    }

    public async Task<IEnumerable<PromotionDto>> GetAllPromotionsAsync()
    {
        var promotions = await context.Promotions
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return promotions.Select(MapToDto);
    }

    public async Task<PromotionDto?> GetPromotionByIdAsync(string promotionId)
    {
        var promotion = await context.Promotions.FindAsync(promotionId);
        return promotion is null ? null : MapToDto(promotion);
    }

    public async Task<PromotionDto> CreatePromotionAsync(CreatePromotionDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto);
        Validate(createDto);

        var promotion = new Promotion
        {
            Title = createDto.Title,
            Description = createDto.Description,
            Category = createDto.Category,
            PromoCode = string.IsNullOrWhiteSpace(createDto.PromoCode) ? null : createDto.PromoCode.Trim().ToUpperInvariant(),
            DiscountPercentage = createDto.DiscountPercentage,
            StartDate = createDto.StartDate,
            EndDate = createDto.EndDate,
            ImageUrl = createDto.ImageUrl,
            IsFeatured = createDto.IsFeatured,
            Status = PromotionStatus.Draft,
            CreatedAt = DateTime.UtcNow,
        };

        await GuardPromoCodeAsync(promotion.PromoCode, promotion.Id);

        context.Promotions.Add(promotion);
        await context.SaveChangesAsync();

        logger.LogInformation("Promotion drafted with id: {PromotionId}", promotion.Id);

        return MapToDto(promotion);
    }

    public async Task<PromotionDto> UpdatePromotionAsync(string promotionId, CreatePromotionDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(updateDto);
        Validate(updateDto);

        var promotion = await context.Promotions.FindAsync(promotionId)
            ?? throw new KeyNotFoundException($"Promotion with id {promotionId} not found");

        promotion.Title = updateDto.Title;
        promotion.Description = updateDto.Description;
        promotion.Category = updateDto.Category;
        promotion.PromoCode = string.IsNullOrWhiteSpace(updateDto.PromoCode) ? null : updateDto.PromoCode.Trim().ToUpperInvariant();
        promotion.DiscountPercentage = updateDto.DiscountPercentage;
        promotion.StartDate = updateDto.StartDate;
        promotion.EndDate = updateDto.EndDate;
        promotion.ImageUrl = updateDto.ImageUrl;
        promotion.IsFeatured = updateDto.IsFeatured;
        promotion.UpdatedAt = DateTime.UtcNow;

        await GuardPromoCodeAsync(promotion.PromoCode, promotion.Id);
        await context.SaveChangesAsync();

        logger.LogInformation("Promotion updated with id: {PromotionId}", promotionId);

        return MapToDto(promotion);
    }

    public async Task<PromotionDto> SetStatusAsync(string promotionId, PromotionStatus status)
    {
        var promotion = await context.Promotions.FindAsync(promotionId)
            ?? throw new KeyNotFoundException($"Promotion with id {promotionId} not found");

        if (status == PromotionStatus.Published && promotion.EndDate < DateTime.UtcNow)
        {
            throw new InvalidOperationException("A promotion that has already ended cannot be published");
        }

        promotion.Status = status;
        promotion.PublishedAt = status == PromotionStatus.Published ? DateTime.UtcNow : promotion.PublishedAt;
        promotion.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("Promotion {PromotionId} moved to {Status}", promotionId, status);

        return MapToDto(promotion);
    }

    public async Task<bool> DeletePromotionAsync(string promotionId)
    {
        var promotion = await context.Promotions.FindAsync(promotionId);
        if (promotion is null)
        {
            return false;
        }

        context.Promotions.Remove(promotion);
        await context.SaveChangesAsync();

        logger.LogInformation("Promotion deleted with id: {PromotionId}", promotionId);

        return true;
    }

    private static void Validate(CreatePromotionDto dto)
    {
        if (dto.DiscountPercentage is < 0 or > 100)
        {
            throw new InvalidOperationException("Discount percentage must be between 0 and 100");
        }

        if (dto.EndDate <= dto.StartDate)
        {
            throw new InvalidOperationException("The promotion must end after it starts");
        }
    }

    private async Task GuardPromoCodeAsync(string? promoCode, string promotionId)
    {
        if (promoCode is null)
        {
            return;
        }

        var taken = await context.Promotions
            .AnyAsync(p => p.PromoCode == promoCode && p.Id != promotionId);

        if (taken)
        {
            throw new InvalidOperationException($"Promo code {promoCode} is already in use");
        }
    }

    private static PromotionDto MapToDto(Promotion promotion) => new()
    {
        Id = promotion.Id,
        Title = promotion.Title,
        Description = promotion.Description,
        Category = promotion.Category,
        PromoCode = promotion.PromoCode,
        DiscountPercentage = promotion.DiscountPercentage,
        StartDate = promotion.StartDate,
        EndDate = promotion.EndDate,
        ImageUrl = promotion.ImageUrl,
        IsFeatured = promotion.IsFeatured,
        Status = promotion.Status,
        PublishedAt = promotion.PublishedAt,
        IsLive = promotion.IsLive(DateTime.UtcNow),
    };
}
