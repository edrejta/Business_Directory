using BusinessDirectory.Application.Dtos.Promotions;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BusinessDirectory.Infrastructure.Services;

public sealed class PromotionService : IPromotionService
{
    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Discounts",
        "FlashSales",
        "EarlyAccess"
    };

    private readonly ApplicationDbContext _db;

    public PromotionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PromotionResponseDto>> GetAsync(GetPromotionsQueryDto query, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var promotions = _db.Promotions
            .AsNoTracking()
            .Include(p => p.Business)
            .AsQueryable();

        if (query.BusinessId.HasValue)
            promotions = promotions.Where(p => p.BusinessId == query.BusinessId.Value);

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = NormalizeCategory(query.Category);
            if (category is null)
                return Array.Empty<PromotionResponseDto>();

            promotions = promotions.Where(p => p.Category == category);
        }

        if (query.OnlyActive)
            promotions = promotions.Where(p => p.IsActive && (p.ExpiresAt == null || p.ExpiresAt >= now));

        return await promotions
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => Map(p, p.Business.BusinessName))
            .ToListAsync(ct);
    }

    public async Task<(PromotionResponseDto? Result, bool NotFound, bool Forbid, string? Error)> CreateAsync(
        Guid actorUserId,
        CreatePromotionRequestDto request,
        CancellationToken ct)
    {
        var business = await _db.Businesses
            .AsNoTracking()
            .Where(b => b.Id == request.BusinessId)
            .Select(b => new { b.Id, b.OwnerId, b.BusinessName })
            .FirstOrDefaultAsync(ct);

        if (business is null)
            return (null, true, false, null);

        if (business.OwnerId != actorUserId)
            return (null, false, true, null);

        var category = NormalizeCategory(request.Category);
        if (category is null)
            return (null, false, false, "Category duhet te jete Discounts, FlashSales ose EarlyAccess.");

        if (request.OriginalPrice.HasValue &&
            request.DiscountedPrice.HasValue &&
            request.DiscountedPrice.Value > request.OriginalPrice.Value)
        {
            return (null, false, false, "DiscountedPrice nuk mund te jete me i madh se OriginalPrice.");
        }

        var entity = new Promotion
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = category,
            OriginalPrice = request.OriginalPrice,
            DiscountedPrice = request.DiscountedPrice,
            ExpiresAt = request.ExpiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Promotions.Add(entity);
        await _db.SaveChangesAsync(ct);

        return (Map(entity, business.BusinessName), false, false, null);
    }

    private static PromotionResponseDto Map(Promotion entity, string businessName)
    {
        return new PromotionResponseDto
        {
            Id = entity.Id,
            BusinessId = entity.BusinessId,
            BusinessName = businessName,
            Title = entity.Title,
            Description = entity.Description,
            Category = entity.Category,
            OriginalPrice = entity.OriginalPrice,
            DiscountedPrice = entity.DiscountedPrice,
            DiscountPercent = CalculateDiscountPercent(entity.OriginalPrice, entity.DiscountedPrice),
            ExpiresAt = entity.ExpiresAt,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    private static string? NormalizeCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return null;

        var normalized = category.Trim();
        return AllowedCategories.FirstOrDefault(c => c.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static int? CalculateDiscountPercent(decimal? originalPrice, decimal? discountedPrice)
    {
        if (originalPrice is null || discountedPrice is null || originalPrice <= 0 || discountedPrice > originalPrice)
            return null;

        var value = (int)Math.Round(((originalPrice.Value - discountedPrice.Value) / originalPrice.Value) * 100m);
        return Math.Clamp(value, 0, 100);
    }
}
