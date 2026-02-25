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
                b.BusinessName.ToLower().Contains(s) ||
                b.Description.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var c = city.Trim().ToLowerInvariant();
            query = query.Where(b => b.City.ToLower() == c);
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
                Address = b.Address,
                City = b.City,
                Email = b.Email,
                PhoneNumber = b.PhoneNumber,
                BusinessType = b.BusinessType,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                BusinessNumber = b.BusinesssNumber
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
                Address = b.Address,
                City = b.City,
                Email = b.Email,
                PhoneNumber = b.PhoneNumber,
                BusinessType = b.BusinessType,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                BusinessNumber = b.BusinesssNumber
            })
            .FirstOrDefaultAsync(ct);

        if (result is not null)
            await SetCacheAsync(cacheKey, result, ApprovedBusinessCacheTtl, ct);

        return result;
    }

    public async Task<BusinessDto?> GetMineByIdAsync(Guid businessId, Guid ownerId, CancellationToken ct)
    {
        return await _db.Businesses
            .AsNoTracking()
            .Where(b => b.Id == businessId && b.OwnerId == ownerId)
            .Select(b => new BusinessDto
            {
                Id = b.Id,
                OwnerId = b.OwnerId,
                BusinessName = b.BusinessName,
                Address = b.Address,
                City = b.City,
                Email = b.Email,
                PhoneNumber = b.PhoneNumber,
                BusinessType = b.BusinessType,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                BusinessNumber = b.BusinesssNumber
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<BusinessDto> CreateAsync(BusinessCreateDto dto, Guid ownerId, CancellationToken ct)
    {
        var business = new Business
        {
            OwnerId = ownerId,
            BusinessName = dto.BusinessName.Trim(),
            Address = dto.Address.Trim(),
            City = dto.City.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            BusinessType = dto.BusinessType,
            Description = dto.Description.Trim(),
            ImageUrl = dto.ImageUrl.Trim(),
            Status = BusinessStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            BusinesssNumber = dto.BusinessNumber,
            WebsiteUrl = string.Empty
        };

        _db.Businesses.Add(business);
        await _db.SaveChangesAsync(ct);
        await BumpVersionAsync(ct);

        return new BusinessDto
        {
            Id = business.Id,
            OwnerId = business.OwnerId,
            BusinessName = business.BusinessName,
            Address = business.Address,
            City = business.City,
            Email = business.Email,
            PhoneNumber = business.PhoneNumber,
            BusinessType = business.BusinessType,
            Description = business.Description,
            ImageUrl = business.ImageUrl,
            Status = business.Status,
            CreatedAt = business.CreatedAt,
            BusinessNumber = business.BusinesssNumber
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

        business.BusinessName = dto.BusinessName.Trim();
        business.Address = dto.Address.Trim();
        business.City = dto.City.Trim();
        business.Email = dto.Email.Trim().ToLowerInvariant();
        business.PhoneNumber = dto.PhoneNumber.Trim();
        business.BusinessType = dto.BusinessType;
        business.Description = dto.Description.Trim();
        business.ImageUrl = dto.ImageUrl.Trim();

        await _db.SaveChangesAsync(ct);
        await BumpVersionAsync(ct);

        return (new BusinessDto
        {
            Id = business.Id,
            OwnerId = business.OwnerId,
            BusinessName = business.BusinessName,
            Address = business.Address,
            City = business.City,
            Email = business.Email,
            PhoneNumber = business.PhoneNumber,
            BusinessType = business.BusinessType,
            Description = business.Description,
            ImageUrl = business.ImageUrl,
            Status = business.Status,
            CreatedAt = business.CreatedAt,
            BusinessNumber = business.BusinesssNumber
        }, false, false, null);
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
                Address = b.Address,
                City = b.City,
                Email = b.Email,
                PhoneNumber = b.PhoneNumber,
                BusinessType = b.BusinessType,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                BusinessNumber = b.BusinesssNumber
            })
            .ToListAsync(ct);
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
