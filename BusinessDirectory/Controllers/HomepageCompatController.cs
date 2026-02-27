using BusinessDirectory.Application.Dtos.Promotions;
using BusinessDirectory.Application.Dtos.Subscribe;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Enums;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusinessDirectory.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api")]
public sealed class HomepageCompatController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPromotionService _promotionService;
    private readonly ISubscribeService _subscribeService;

    public HomepageCompatController(
        ApplicationDbContext db,
        IPromotionService promotionService,
        ISubscribeService subscribeService)
    {
        _db = db;
        _promotionService = promotionService;
        _subscribeService = subscribeService;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories(CancellationToken ct)
    {
        var categories = await _db.Businesses
            .AsNoTracking()
            .Where(b => b.Status == BusinessStatus.Approved)
            .Select(b => b.BusinessType)
            .Distinct()
            .OrderBy(x => x)
            .Select(x => x.ToString())
            .ToListAsync(ct);

        return Ok(categories);
    }

    [HttpGet("locations")]
    public async Task<ActionResult<List<string>>> GetLocations(CancellationToken ct)
    {
        var locations = await _db.Businesses
            .AsNoTracking()
            .Where(b => b.Status == BusinessStatus.Approved && b.City != null && b.City != "")
            .Select(b => b.City.Trim())
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);

        return Ok(locations);
    }

    [HttpGet("featured-businesses")]
    public async Task<ActionResult<List<HomeBusinessDto>>> GetFeatured([FromQuery] int limit = 10, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        var items = await _db.Businesses
            .AsNoTracking()
            .Where(b => b.Status == BusinessStatus.Approved)
            .OrderByDescending(b => b.CreatedAt)
            .Take(limit)
            .Select(b => new
            {
                b.Id,
                b.BusinessName,
                b.ImageUrl,
                b.Description,
                b.BusinessType,
                b.City,
                b.PhoneNumber,
                b.CreatedAt
            })
            .ToListAsync(ct);

        if (items.Count == 0)
            return Ok(new List<HomeBusinessDto>());

        var ratings = await _db.Comments
            .AsNoTracking()
            .Where(c => items.Select(x => x.Id).Contains(c.BusinessId))
            .GroupBy(c => c.BusinessId)
            .Select(g => new { BusinessId = g.Key, Rating = g.Average(x => (double)x.Rate) })
            .ToDictionaryAsync(x => x.BusinessId, x => x.Rating, ct);

        return Ok(items.Select(x => MapHomeBusiness(
            x.Id,
            x.BusinessName,
            x.ImageUrl,
            x.Description,
            ratings.GetValueOrDefault(x.Id, 0),
            x.BusinessType.ToString(),
            x.City,
            x.PhoneNumber,
            x.CreatedAt)).ToList());
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<HomeBusinessDto>>> Search(
        [FromQuery] string? keyword = null,
        [FromQuery] string? category = null,
        [FromQuery] string? location = null,
        [FromQuery] int limit = 20,
        [FromQuery] int page = 1,
        [FromQuery] string? sortBy = null,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        page = Math.Max(1, page);

        var query = _db.Businesses
            .AsNoTracking()
            .Where(b => b.Status == BusinessStatus.Approved)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var s = $"%{keyword.Trim()}%";
            query = query.Where(b =>
                EF.Functions.Like(b.BusinessName, s) ||
                EF.Functions.Like(b.Description, s) ||
                EF.Functions.Like(b.City, s) ||
                EF.Functions.Like(b.Address, s));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var types = category
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => Enum.TryParse<BusinessType>(x, true, out var parsed) ? parsed : (BusinessType?)null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();

            if (types.Count == 0)
                return Ok(new List<HomeBusinessDto>());

            query = query.Where(b => types.Contains(b.BusinessType));
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var cities = location
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.ToLowerInvariant())
                .Distinct()
                .ToList();
            query = query.Where(b => cities.Contains((b.City ?? string.Empty).ToLower()));
        }

        if (string.Equals(sortBy, "rating", StringComparison.OrdinalIgnoreCase))
        {
            var response = await query
                .Select(b => new
                {
                    b.Id,
                    b.BusinessName,
                    b.ImageUrl,
                    b.Description,
                    b.BusinessType,
                    b.City,
                    b.PhoneNumber,
                    b.CreatedAt,
                    Rating = _db.Comments
                        .Where(c => c.BusinessId == b.Id)
                        .Select(c => (double?)c.Rate)
                        .Average() ?? 0d
                })
                .OrderByDescending(x => x.Rating)
                .ThenByDescending(x => x.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(ct);

            return Ok(response.Select(x => MapHomeBusiness(
                x.Id,
                x.BusinessName,
                x.ImageUrl,
                x.Description,
                x.Rating,
                x.BusinessType.ToString(),
                x.City,
                x.PhoneNumber,
                x.CreatedAt)).ToList());
        }

        var projected = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(b => new
            {
                b.Id,
                b.BusinessName,
                b.ImageUrl,
                b.Description,
                b.BusinessType,
                b.City,
                b.PhoneNumber,
                b.CreatedAt
            })
            .ToListAsync(ct);

        if (projected.Count == 0)
            return Ok(new List<HomeBusinessDto>());

        var ratings = await _db.Comments
            .AsNoTracking()
            .Where(c => projected.Select(x => x.Id).Contains(c.BusinessId))
            .GroupBy(c => c.BusinessId)
            .Select(g => new { BusinessId = g.Key, Rating = g.Average(x => (double)x.Rate) })
            .ToDictionaryAsync(x => x.BusinessId, x => x.Rating, ct);

        return Ok(projected.Select(x => MapHomeBusiness(
            x.Id,
            x.BusinessName,
            x.ImageUrl,
            x.Description,
            ratings.GetValueOrDefault(x.Id, 0),
            x.BusinessType.ToString(),
            x.City,
            x.PhoneNumber,
            x.CreatedAt)).ToList());
    }

    [HttpGet("/promotions")]
    public async Task<ActionResult<IReadOnlyList<PromotionResponseDto>>> GetPromotions(
        [FromQuery] string? category = null,
        [FromQuery] Guid? businessId = null,
        [FromQuery] bool onlyActive = true,
        CancellationToken ct = default)
    {
        var data = await _promotionService.GetAsync(new GetPromotionsQueryDto
        {
            Category = category,
            BusinessId = businessId,
            OnlyActive = onlyActive
        }, ct);

        return Ok(data);
    }

    [HttpPost("/subscribe")]
    public async Task<ActionResult<SubscribeResponseDto>> Subscribe([FromBody] SubscribeRequestDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await _subscribeService.SubscribeAsync(request, ct);
        return Ok(result);
    }

    public sealed class HomeBusinessDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Logo { get; set; }
        public string? Description { get; set; }
        public double Rating { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }
        public string? Phone { get; set; }
        public HomeCoordinatesDto? Coordinates { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed class HomeCoordinatesDto
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    private static HomeBusinessDto MapHomeBusiness(
        Guid id,
        string name,
        string? logo,
        string? description,
        double rating,
        string? category,
        string? location,
        string? phone,
        DateTime createdAt)
    {
        return new HomeBusinessDto
        {
            Id = id,
            Name = name,
            Logo = logo,
            Description = description,
            Rating = rating,
            Category = category,
            Location = location,
            Phone = phone,
            Coordinates = null,
            CreatedAt = createdAt
        };
    }
}
