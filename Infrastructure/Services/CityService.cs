using BusinessDirectory.Application.Dtos.City;
using BusinessDirectory.Application.Interfaces;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BusinessDirectory.Infrastructure.Services;

public sealed class CityService : ICityService
{
    private static readonly JsonSerializerOptions CacheJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan CitiesAllCacheTtl = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan CitiesSearchCacheTtl = TimeSpan.FromMinutes(30);

    private readonly ApplicationDbContext _db;
    private readonly IDistributedCache _cache;

    public CityService(ApplicationDbContext db, IDistributedCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<List<CityDto>> GetAllAsync(CancellationToken ct)
    {
        const string cacheKey = "cities:all:v1";
        var cached = await GetFromCacheAsync<IReadOnlyList<CityDto>>(cacheKey, ct);
        if (cached is not null)
            return cached.ToList();

        var result = await _db.Cities.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CityDto { Id = c.Id, Name = c.Name })
            .ToListAsync(ct);

        await SetCacheAsync(cacheKey, result, CitiesAllCacheTtl, ct);
        return result;
    }

    public async Task<List<CityDto>> SearchAsync(string? search, int take, CancellationToken ct)
    {
        take = take <= 0 ? 20 : Math.Min(take, 50);
        var cacheKey = $"cities:search:v1:query={NormalizeCacheSegment(search)}:take={take}";
        var cached = await GetFromCacheAsync<IReadOnlyList<CityDto>>(cacheKey, ct);
        if (cached is not null)
            return cached.ToList();

        var query = _db.Cities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(c => c.Name.Contains(s));
        }

        var result = await query
            .OrderBy(c => c.Name)
            .Take(take)
            .Select(c => new CityDto { Id = c.Id, Name = c.Name })
            .ToListAsync(ct);

        await SetCacheAsync(cacheKey, result, CitiesSearchCacheTtl, ct);
        return result;
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
