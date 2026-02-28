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

    private static BusinessType ParseBusinessTypeOrUnknown(string? type, BusinessType? businessType = null)
    {
        if (businessType.HasValue && businessType.Value != BusinessType.Unknown)
            return businessType.Value;

        if (string.IsNullOrWhiteSpace(type))
            return BusinessType.Unknown;

        return Enum.TryParse<BusinessType>(type.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : BusinessType.Unknown;
    }

    private static string OpenDaysFromMask(int mask)
    {
        var days = new List<string>(7);
        if ((mask & 1) != 0) days.Add("Mon");
        if ((mask & 2) != 0) days.Add("Tue");
        if ((mask & 4) != 0) days.Add("Wed");
        if ((mask & 8) != 0) days.Add("Thu");
        if ((mask & 16) != 0) days.Add("Fri");
        if ((mask & 32) != 0) days.Add("Sat");
        if ((mask & 64) != 0) days.Add("Sun");
        return days.Count == 0 ? string.Empty : string.Join(", ", days);
    }

    private static int? TryParseOpenDaysToMask(string? input)
    {
        if (input is null)
            return null;

        var trimmed = input.Trim();
        if (trimmed.Length == 0)
            return 127;

        if (int.TryParse(trimmed, out var numeric))
        {
            if (numeric < 0) return 0;
            if (numeric > 127) return 127;
            return numeric;
        }

        var tokens = trimmed
            .Replace(";", ",")
            .Replace("|", ",")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length == 0)
            return 127;

        var mask = 0;
        foreach (var raw in tokens)
        {
            var t = raw.Trim().ToLowerInvariant();

            if (t is "mon" or "monday") mask |= 1;
            else if (t is "tue" or "tues" or "tuesday") mask |= 2;
            else if (t is "wed" or "wednesday") mask |= 4;
            else if (t is "thu" or "thur" or "thurs" or "thursday") mask |= 8;
            else if (t is "fri" or "friday") mask |= 16;
            else if (t is "sat" or "saturday") mask |= 32;
            else if (t is "sun" or "sunday") mask |= 64;
        }

        return mask == 0 ? 127 : mask;
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
                BusinessType = b.BusinessType,

                Address = b.Address,
                City = b.City,
                Email = b.Email,
                PhoneNumber = b.PhoneNumber,
                OpenDays = OpenDaysFromMask(b.OpenDaysMask),

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
                BusinessType = b.BusinessType,
                Address = b.Address,
                City = b.City,
                Email = b.Email,
                PhoneNumber = b.PhoneNumber,
                OpenDays = OpenDaysFromMask(b.OpenDaysMask),
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
                BusinessType = b.BusinessType,
                Address = b.Address,
                City = b.City,
                Email = b.Email,
                PhoneNumber = b.PhoneNumber,
                OpenDays = OpenDaysFromMask(b.OpenDaysMask),
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
        var parsedType = ParseBusinessTypeOrUnknown(dto.Type, dto.BusinessType);
        var openDaysMask = TryParseOpenDaysToMask(dto.OpenDays) ?? 127;

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

            Email = TrimOrEmpty(dto.Email),

            OpenDaysMask = openDaysMask,

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
            BusinessType = business.BusinessType,
            Address = business.Address,
            City = business.City,
            Email = business.Email,
            PhoneNumber = business.PhoneNumber,
            OpenDays = OpenDaysFromMask(business.OpenDaysMask),
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
        business.BusinessType = ParseBusinessTypeOrUnknown(dto.Type, dto.BusinessType);
        business.City = TrimOrEmpty(dto.City);
        business.Address = TrimOrEmpty(dto.Address);
        business.Description = TrimOrEmpty(dto.Description);
        business.PhoneNumber = TrimOrEmpty(dto.PhoneNumber);
        business.ImageUrl = TrimOrEmpty(dto.ImageUrl);
        business.WebsiteUrl = TrimOrEmpty(dto.BusinessUrl);

        if (dto.Email is not null)
            business.Email = TrimOrEmpty(dto.Email);

        var parsedMask = TryParseOpenDaysToMask(dto.OpenDays);
        if (parsedMask.HasValue)
            business.OpenDaysMask = parsedMask.Value;

        await _db.SaveChangesAsync(ct);
        await BumpVersionAsync(ct);

        return (new BusinessDto
        {
            Id = business.Id,
            OwnerId = business.OwnerId,
            BusinessName = business.BusinessName,
            Type = business.BusinessType.ToString(),
            BusinessType = business.BusinessType,
            Address = business.Address,
            City = business.City,
            Email = business.Email,
            PhoneNumber = business.PhoneNumber,
            OpenDays = OpenDaysFromMask(business.OpenDaysMask),
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
