using BusinessDirectory.Application.Dtos.Promotions;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
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

    public PromotionService(ApplicationDbContext db, IDistributedCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<PromotionResponseDto>> GetAsync(GetPromotionsQueryDto query, CancellationToken ct)
    {
        var version = await GetVersionAsync(ct);
        var cacheKey = $"promotions:{version}:businessId={(query.BusinessId?.ToString() ?? "null")}:category={NormalizeCacheSegment(query.Category)}:active={query.OnlyActive}";
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
            promotions = promotions.Where(p => p.IsActive && (p.ExpiresAt == null || p.ExpiresAt >= now));

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

        var title = request.Title?.Trim() ?? string.Empty;
        var description = request.Description?.Trim() ?? string.Empty;
        if (title.Length == 0)
            return (null, false, false, "Title eshte i detyrueshem.");
        if (description.Length == 0)
            return (null, false, false, "Description eshte i detyrueshem.");

        var category = NormalizeCategory(request.Category);
        if (category is null)
            return (null, false, false, "Category duhet te jete Discounts, FlashSales ose EarlyAccess.");

        if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTime.UtcNow)
            return (null, false, false, "ExpiresAt duhet te jete ne te ardhmen.");

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
            Title = title,
            Description = description,
            Category = category,
            OriginalPrice = request.OriginalPrice,
            DiscountedPrice = request.DiscountedPrice,
            ExpiresAt = request.ExpiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Promotions.Add(entity);
        await _db.SaveChangesAsync(ct);
        await BumpVersionAsync(ct);

        return (Map(entity, business.BusinessName), false, false, null);
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
        }
    }

    private async Task<T?> GetFromCacheAsync<T>(string key, CancellationToken ct)
    {
        try
        {
            var json = await _cache.GetStringAsync(key, ct);
            return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, CacheJsonOptions);
        }
        catch
        {
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
        catch
        {
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
