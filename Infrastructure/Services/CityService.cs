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

    private readonly ApplicationDbContext _db;
    private readonly IDistributedCache _cache;

    public CityService(ApplicationDbContext db, IDistributedCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<CityDto>> GetAllAsync(CancellationToken ct)
    {
        const string cacheKey = "cities:all:v1";
        var cached = await GetFromCacheAsync<IReadOnlyList<CityDto>>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var result = await _db.Cities.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CityDto { Id = c.Id, Name = c.Name })
            .ToListAsync(ct);

        await SetCacheAsync(cacheKey, result, CitiesAllCacheTtl, ct);
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

}
