using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Dtos.Businesses;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Entities;
using BusinessDirectory.Domain.Enums;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BusinessDirectory.Infrastructure.Services;

public class BusinessService : IBusinessService
{
    private const string BusinessCacheVersionKey = "cache:businesses:version";
    private static readonly TimeSpan ApprovedBusinessCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions CacheJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _db;
    private readonly IDistributedCache _cache;

    public BusinessService(ApplicationDbContext db, IDistributedCache cache)
    {
        _db = db;
        _cache = cache;
    }

    private static string TrimOrEmpty(string? value) => value?.Trim() ?? string.Empty;

    private static BusinessType ParseBusinessTypeOrUnknown(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return BusinessType.Unknown;

        return Enum.TryParse<BusinessType>(type.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : BusinessType.Unknown;
    }

    public async Task<IReadOnlyList<BusinessDto>> GetApprovedAsync(
        string? search,
        string? city,
        BusinessType? type,
        CancellationToken ct)
    {
        var version = await GetVersionAsync(ct);
        var cacheKey = $"businesses:approved:{version}:search={NormalizeCacheSegment(search)}:city={NormalizeCacheSegment(city)}:type={(type?.ToString() ?? "null")}";
        var cached = await GetFromCacheAsync<IReadOnlyList<BusinessDto>>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var query = _db.Businesses.AsNoTracking()
            .Where(b => b.Status == BusinessStatus.Approved);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(b =>
                (b.BusinessName ?? string.Empty).ToLower().Contains(s) ||
                (b.Description ?? string.Empty).ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var c = city.Trim().ToLowerInvariant();
            query = query.Where(b => (b.City ?? string.Empty).ToLower() == c);
        }

        if (type.HasValue && type.Value != BusinessType.Unknown)
        {
            query = query.Where(b => b.BusinessType == type.Value);
        }

        var result = await query
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BusinessDto
            {
                Id = b.Id,
                OwnerId = b.OwnerId,

                BusinessName = b.BusinessName,
                Type = b.BusinessType.ToString(),

                Address = b.Address,
                City = b.City,
                Email = b.Email,
                PhoneNumber = b.PhoneNumber,
                Description = b.Description,
                ImageUrl = b.ImageUrl,

                BusinessUrl = b.WebsiteUrl,

                Status = b.Status,
                CreatedAt = b.CreatedAt,
                BusinessNumber = b.BusinesssNumber,

                SuspensionReason = b.SuspensionReason,
                IsFavorite = false
            })
            .ToListAsync(ct);

        await SetCacheAsync(cacheKey, result, ApprovedBusinessCacheTtl, ct);
        return result;
    }

    public async Task<BusinessDto?> GetApprovedByIdAsync(Guid id, CancellationToken ct)
    {
        var version = await GetVersionAsync(ct);
        var cacheKey = $"businesses:approved:{version}:id={id}";
        var cached = await GetFromCacheAsync<BusinessDto>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var result = await _db.Businesses.AsNoTracking()
            .Where(b => b.Id == id && b.Status == BusinessStatus.Approved)
            .Select(b => new BusinessDto
            {
                Id = b.Id,
                OwnerId = b.OwnerId,
                BusinessName = b.BusinessName,
                Type = b.BusinessType.ToString(),
                Address = b.Address,
                City = b.City,
                Email = b.Email,
                PhoneNumber = b.PhoneNumber,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                BusinessUrl = b.WebsiteUrl,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                BusinessNumber = b.BusinesssNumber,
                SuspensionReason = b.SuspensionReason,
                IsFavorite = false
            })
            .FirstOrDefaultAsync(ct);

        if (result is not null)
            await SetCacheAsync(cacheKey, result, ApprovedBusinessCacheTtl, ct);

        return result;
    }

    public async Task<BusinessDto?> GetMineByIdAsync(Guid businessId, Guid ownerId, CancellationToken ct)
    {
        return await _db.Businesses.AsNoTracking()
            .Where(b => b.Id == businessId && b.OwnerId == ownerId)
            .Select(b => new BusinessDto
            {
                Id = b.Id,
                OwnerId = b.OwnerId,
                BusinessName = b.BusinessName,
                Type = b.BusinessType.ToString(),
                Address = b.Address,
                City = b.City,
                Email = b.Email,
                PhoneNumber = b.PhoneNumber,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                BusinessUrl = b.WebsiteUrl,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                BusinessNumber = b.BusinesssNumber,
                SuspensionReason = b.SuspensionReason,
                IsFavorite = false
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<BusinessDto>> GetMineAsync(
        Guid ownerId,
        BusinessStatus? status,
        CancellationToken ct)
    {
        var query = _db.Businesses.AsNoTracking()
            .Where(b => b.OwnerId == ownerId);

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BusinessDto
            {
                Id = b.Id,
                OwnerId = b.OwnerId,
                BusinessName = b.BusinessName,
                Type = b.BusinessType.ToString(),
                Address = b.Address,
                City = b.City,
                Email = b.Email,
                PhoneNumber = b.PhoneNumber,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                BusinessUrl = b.WebsiteUrl,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                BusinessNumber = b.BusinesssNumber,
                SuspensionReason = b.SuspensionReason,
                IsFavorite = false
            })
            .ToListAsync(ct);
    }

    public async Task<BusinessDto> CreateAsync(BusinessCreateDto dto, Guid ownerId, CancellationToken ct)
    {
        var parsedType = ParseBusinessTypeOrUnknown(dto.Type);

        var business = new Business
        {
            OwnerId = ownerId,

            BusinessName = TrimOrEmpty(dto.BusinessName),
            BusinessType = parsedType,

            City = TrimOrEmpty(dto.City),
            Address = TrimOrEmpty(dto.Address),
            Description = TrimOrEmpty(dto.Description),
            PhoneNumber = TrimOrEmpty(dto.PhoneNumber),
            ImageUrl = TrimOrEmpty(dto.ImageUrl),

            WebsiteUrl = TrimOrEmpty(dto.BusinessUrl),

            BusinesssNumber = TrimOrEmpty(dto.BusinessNumber),

            Email = string.Empty,

            Status = BusinessStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.Businesses.Add(business);
        await _db.SaveChangesAsync(ct);
        await BumpVersionAsync(ct);

        return new BusinessDto
        {
            Id = business.Id,
            OwnerId = business.OwnerId,
            BusinessName = business.BusinessName,
            Type = business.BusinessType.ToString(),
            Address = business.Address,
            City = business.City,
            Email = business.Email,
            PhoneNumber = business.PhoneNumber,
            Description = business.Description,
            ImageUrl = business.ImageUrl,
            BusinessUrl = business.WebsiteUrl,
            Status = business.Status,
            CreatedAt = business.CreatedAt,
            BusinessNumber = business.BusinesssNumber,
            SuspensionReason = business.SuspensionReason,
            IsFavorite = false
        };
    }

    public async Task<(BusinessDto? Result, bool NotFound, bool Forbid, string? Error)> UpdateAsync(
        Guid id,
        BusinessUpdateDto dto,
        Guid ownerId,
        CancellationToken ct)
    {
        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == id, ct);

        if (business is null)
            return (null, true, false, null);

        if (business.OwnerId != ownerId)
            return (null, false, true, null);

        if (business.Status is not (BusinessStatus.Pending or BusinessStatus.Rejected))
            return (null, false, false, "Business mund të përditësohet vetëm kur është Pending ose Rejected.");

        business.BusinessName = TrimOrEmpty(dto.BusinessName);
        business.BusinessType = ParseBusinessTypeOrUnknown(dto.Type);
        business.City = TrimOrEmpty(dto.City);
        business.Address = TrimOrEmpty(dto.Address);
        business.Description = TrimOrEmpty(dto.Description);
        business.PhoneNumber = TrimOrEmpty(dto.PhoneNumber);
        business.ImageUrl = TrimOrEmpty(dto.ImageUrl);

        business.WebsiteUrl = TrimOrEmpty(dto.BusinessUrl);

        await _db.SaveChangesAsync(ct);
        await BumpVersionAsync(ct);

        return (new BusinessDto
        {
            Id = business.Id,
            OwnerId = business.OwnerId,
            BusinessName = business.BusinessName,
            Type = business.BusinessType.ToString(),
            Address = business.Address,
            City = business.City,
            Email = business.Email,
            PhoneNumber = business.PhoneNumber,
            Description = business.Description,
            ImageUrl = business.ImageUrl,
            BusinessUrl = business.WebsiteUrl,
            Status = business.Status,
            CreatedAt = business.CreatedAt,
            BusinessNumber = business.BusinesssNumber,
            SuspensionReason = business.SuspensionReason,
            IsFavorite = false
        }, false, false, null);
    }

    public async Task<(bool NotFound, bool Forbid, string? Error)> DeleteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken ct)
    {
        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == id, ct);

        if (business is null)
            return (true, false, null);

        if (business.OwnerId != ownerId)
            return (false, true, null);

        if (business.Status is not (BusinessStatus.Pending or BusinessStatus.Rejected))
            return (false, false, "Business mund të fshihet vetëm kur është Pending ose Rejected.");

        _db.Businesses.Remove(business);
        await _db.SaveChangesAsync(ct);
        await BumpVersionAsync(ct);

        return (false, false, null);
    }

    private async Task<string> GetVersionAsync(CancellationToken ct)
    {
        try
        {
            var version = await _cache.GetStringAsync(BusinessCacheVersionKey, ct);
            if (!string.IsNullOrWhiteSpace(version))
                return version;

            version = "v1";
            await _cache.SetStringAsync(
                BusinessCacheVersionKey,
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
                BusinessCacheVersionKey,
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
}
