using BusinessDirectory.Application.Dtos.Promotions;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BusinessDirectory.Infrastructure.Services;

public sealed class PromotionService : IPromotionService
{
    private const string PromotionsCacheVersionKey = "cache:promotions:version";
    private static readonly TimeSpan PromotionsCacheTtl = TimeSpan.FromMinutes(3);
    private static readonly JsonSerializerOptions CacheJsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Discounts",
        "FlashSales",
        "EarlyAccess"
    };

    private readonly ApplicationDbContext _db;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PromotionService> _logger;

    public PromotionService(
        ApplicationDbContext db,
        IDistributedCache cache,
        ILogger<PromotionService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PromotionResponseDto>> GetAsync(GetPromotionsQueryDto query, CancellationToken ct)
    {
        var version = await GetVersionAsync(ct);
        var cacheKey =
            $"promotions:{version}:businessId={(query.BusinessId?.ToString() ?? "null")}:category={NormalizeCacheSegment(query.Category)}:active={query.OnlyActive}";
        var cached = await GetFromCacheAsync<IReadOnlyList<PromotionResponseDto>>(cacheKey, ct);
        if (cached is not null)
            return cached;

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
        {
            promotions = promotions.Where(p =>
                p.IsActive &&
                (p.StartsAt == null || p.StartsAt <= now) &&
                (p.ExpiresAt == null || p.ExpiresAt >= now));
        }

        var result = await promotions
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => Map(p, p.Business.BusinessName))
            .ToListAsync(ct);

        await SetCacheAsync(cacheKey, result, PromotionsCacheTtl, ct);
        return result;
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

        var validation = ValidateAndNormalizeRequest(
            request.Title,
            request.Description,
            request.Category,
            request.StartsAt,
            request.ExpiresAt,
            request.OriginalPrice,
            request.DiscountedPrice,
            requireFutureExpiry: true);
        if (validation.Error is not null)
            return (null, false, false, validation.Error);

        var entity = new Promotion
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            Title = validation.Title,
            Description = validation.Description,
            Category = validation.Category!,
            OriginalPrice = request.OriginalPrice,
            DiscountedPrice = request.DiscountedPrice,
            StartsAt = request.StartsAt,
            ExpiresAt = request.ExpiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Promotions.Add(entity);
        await _db.SaveChangesAsync(ct);
        await BumpVersionAsync(ct);

        return (Map(entity, business.BusinessName), false, false, null);
    }

    public async Task<IReadOnlyList<PromotionResponseDto>> GetMineAsync(
        Guid actorUserId,
        Guid? businessId,
        string? category,
        CancellationToken ct)
    {
        var promotions = _db.Promotions
            .AsNoTracking()
            .Include(p => p.Business)
            .Where(p => p.Business.OwnerId == actorUserId)
            .AsQueryable();

        if (businessId.HasValue)
            promotions = promotions.Where(p => p.BusinessId == businessId.Value);

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = NormalizeCategory(category);
            if (normalized is null)
                return Array.Empty<PromotionResponseDto>();

            promotions = promotions.Where(p => p.Category == normalized);
        }

        var result = await promotions
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => Map(p, p.Business.BusinessName))
            .ToListAsync(ct);

        return result;
    }

    public async Task<(PromotionResponseDto? Result, bool NotFound, bool Forbid, string? Error)> UpdateAsync(
        Guid actorUserId,
        Guid promotionId,
        UpdatePromotionRequestDto request,
        CancellationToken ct)
    {
        var promotion = await _db.Promotions
            .Include(p => p.Business)
            .FirstOrDefaultAsync(p => p.Id == promotionId, ct);

        if (promotion is null)
            return (null, true, false, null);

        if (promotion.Business.OwnerId != actorUserId)
            return (null, false, true, null);

        var validation = ValidateAndNormalizeRequest(
            request.Title,
            request.Description,
            request.Category,
            request.StartsAt,
            request.ExpiresAt,
            request.OriginalPrice,
            request.DiscountedPrice,
            requireFutureExpiry: false);
        if (validation.Error is not null)
            return (null, false, false, validation.Error);

        promotion.Title = validation.Title;
        promotion.Description = validation.Description;
        promotion.Category = validation.Category!;
        promotion.OriginalPrice = request.OriginalPrice;
        promotion.DiscountedPrice = request.DiscountedPrice;
        promotion.StartsAt = request.StartsAt;
        promotion.ExpiresAt = request.ExpiresAt;
        promotion.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);
        await BumpVersionAsync(ct);

        return (Map(promotion, promotion.Business.BusinessName), false, false, null);
    }

    public async Task<(bool NotFound, bool Forbid, string? Error)> DeleteAsync(
        Guid actorUserId,
        Guid promotionId,
        CancellationToken ct)
    {
        var promotion = await _db.Promotions
            .Include(p => p.Business)
            .FirstOrDefaultAsync(p => p.Id == promotionId, ct);

        if (promotion is null)
            return (true, false, null);

        if (promotion.Business.OwnerId != actorUserId)
            return (false, true, null);

        _db.Promotions.Remove(promotion);
        await _db.SaveChangesAsync(ct);
        await BumpVersionAsync(ct);

        return (false, false, null);
    }

    private async Task<string> GetVersionAsync(CancellationToken ct)
    {
        try
        {
            var version = await _cache.GetStringAsync(PromotionsCacheVersionKey, ct);
            if (!string.IsNullOrWhiteSpace(version))
                return version;

            version = "v1";
            await _cache.SetStringAsync(
                PromotionsCacheVersionKey,
                version,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) },
                ct);

            return version;
        }
        catch
        {
            _logger.LogWarning("Falling back to default promotions cache version due to cache read/write error.");
            return "v1";
        }
    }

    private async Task BumpVersionAsync(CancellationToken ct)
    {
        try
        {
            var nextVersion = $"v{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            await _cache.SetStringAsync(
                PromotionsCacheVersionKey,
                nextVersion,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) },
                ct);
        }
        catch
        {
            _logger.LogWarning("Failed to bump promotions cache version.");
        }
    }

    private async Task<T?> GetFromCacheAsync<T>(string key, CancellationToken ct)
    {
        try
        {
            var json = await _cache.GetStringAsync(key, ct);
            return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, CacheJsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read promotions data from cache key {CacheKey}.", key);
            return default;
        }
    }

    private async Task SetCacheAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, CacheJsonOptions);
            await _cache.SetStringAsync(
                key,
                json,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write promotions data to cache key {CacheKey}.", key);
        }
    }

    private static string NormalizeCacheSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "null";

        return Uri.EscapeDataString(value.Trim().ToLowerInvariant());
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
            StartsAt = entity.StartsAt,
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

    private static (string Title, string Description, string? Category, string? Error) ValidateAndNormalizeRequest(
        string? titleInput,
        string? descriptionInput,
        string? categoryInput,
        DateTime? startsAt,
        DateTime? expiresAt,
        decimal? originalPrice,
        decimal? discountedPrice,
        bool requireFutureExpiry)
    {
        var title = titleInput?.Trim() ?? string.Empty;
        if (title.Length == 0)
            return (string.Empty, string.Empty, null, "Title eshte i detyrueshem.");

        var description = descriptionInput?.Trim() ?? string.Empty;
        if (description.Length == 0)
            return (string.Empty, string.Empty, null, "Description eshte i detyrueshem.");

        var category = NormalizeCategory(categoryInput);
        if (category is null)
            return (string.Empty, string.Empty, null, "Category duhet te jete Discounts, FlashSales ose EarlyAccess.");

        if (startsAt.HasValue && expiresAt.HasValue && expiresAt.Value <= startsAt.Value)
            return (string.Empty, string.Empty, null, "ExpiresAt duhet te jete pas StartsAt.");

        if (requireFutureExpiry && expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
            return (string.Empty, string.Empty, null, "ExpiresAt duhet te jete ne te ardhmen.");

        if (originalPrice.HasValue &&
            discountedPrice.HasValue &&
            discountedPrice.Value > originalPrice.Value)
        {
            return (string.Empty, string.Empty, null, "DiscountedPrice nuk mund te jete me i madh se OriginalPrice.");
        }

        return (title, description, category, null);
    }
}
