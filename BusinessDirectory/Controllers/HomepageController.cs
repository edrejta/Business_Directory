using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Domain.Entities;
using BusinessDirectory.Domain.Enums;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("")]
public sealed class HomepageController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private const string DealMetaPrefix = "[DEAL_META]";
    private static readonly Dictionary<string, CoordinatesDto> KosovoCityCoordinates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Prishtine"] = new CoordinatesDto { Lat = 42.6629, Lng = 21.1655 },
        ["Prizren"] = new CoordinatesDto { Lat = 42.2139, Lng = 20.7397 },
        ["Peje"] = new CoordinatesDto { Lat = 42.6598, Lng = 20.2883 },
        ["Gjakove"] = new CoordinatesDto { Lat = 42.3803, Lng = 20.4308 },
        ["Gjilan"] = new CoordinatesDto { Lat = 42.4635, Lng = 21.4694 },
        ["Ferizaj"] = new CoordinatesDto { Lat = 42.3702, Lng = 21.1558 },
        ["Mitrovice"] = new CoordinatesDto { Lat = 42.8914, Lng = 20.8660 },
        ["Vushtrri"] = new CoordinatesDto { Lat = 42.8231, Lng = 20.9675 },
        ["Podujeve"] = new CoordinatesDto { Lat = 42.9106, Lng = 21.1925 },
        ["Suhareke"] = new CoordinatesDto { Lat = 42.3583, Lng = 20.8250 },
    };

    public HomepageController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<List<string>>> GetCategories(CancellationToken cancellationToken)
    {
        var categories = (await _context.Businesses
            .AsNoTracking()
            .Where(b => b.Status == BusinessStatus.Approved)
            .Select(b => b.BusinessType)
            .Distinct()
            .ToListAsync(cancellationToken))
            .Select(bt => bt.ToString())
            .OrderBy(x => x)
            .ToList();

        return Ok(categories);
    }

    [HttpGet("locations")]
    [AllowAnonymous]
    public async Task<ActionResult<List<string>>> GetLocations(CancellationToken cancellationToken)
    {
        var locations = await _context.Businesses
            .AsNoTracking()
            .Where(b => b.Status == BusinessStatus.Approved)
            .Select(b => b.City.Trim())
            .Where(c => c != string.Empty)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return Ok(locations);
    }

    [HttpGet("featured-businesses")]
    [AllowAnonymous]
    public async Task<ActionResult<List<PublicBusinessDto>>> GetFeaturedBusinesses([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var query = _context.Businesses
            .AsNoTracking()
            .Where(b => b.Status == BusinessStatus.Approved && b.Featured)
            .Take(limit);

        return Ok(await ProjectPublicBusinesses(query, cancellationToken));
    }

    [HttpGet("recommendations")]
    [AllowAnonymous]
    public async Task<ActionResult<List<PublicBusinessDto>>> GetRecommendations(
        [FromQuery] int limit = 20,
        [FromQuery] string? category = null,
        [FromQuery] string? location = null,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        var query = _context.Businesses
            .AsNoTracking()
            .Where(b => b.Status == BusinessStatus.Approved);

        var requestedCategories = ParseBusinessTypes(category);
        if (requestedCategories.Count > 0)
        {
            query = query.Where(b => requestedCategories.Contains(b.BusinessType));
        }

        var requestedLocations = ParseLocations(location);
        if (requestedLocations.Count > 0)
        {
            query = query.Where(b => requestedLocations.Contains(b.City.ToLower()));
        }

        // If user is authenticated and no explicit filters are passed, infer preferences from their review history.
        if (requestedCategories.Count == 0 && requestedLocations.Count == 0 &&
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            var preferredCategories = await _context.Comments
                .AsNoTracking()
                .Where(c => c.UserId == userId && c.Business.Status == BusinessStatus.Approved)
                .GroupBy(c => c.Business.BusinessType)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(3)
                .ToListAsync(cancellationToken);

            var preferredLocations = await _context.Comments
                .AsNoTracking()
                .Where(c => c.UserId == userId && c.Business.Status == BusinessStatus.Approved)
                .GroupBy(c => c.Business.City.ToLower())
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(3)
                .ToListAsync(cancellationToken);

            if (preferredCategories.Count > 0 || preferredLocations.Count > 0)
            {
                var personalized = query.Where(b =>
                    preferredCategories.Contains(b.BusinessType) ||
                    preferredLocations.Contains(b.City.ToLower()));

                var personalizedList = await ProjectPublicBusinesses(
                    personalized.OrderByDescending(b => b.CreatedAt).Take(limit),
                    cancellationToken);

                if (personalizedList.Count > 0)
                    return Ok(personalizedList);
            }
        }

        return Ok(await ProjectPublicBusinesses(
            query.OrderByDescending(b => b.CreatedAt).Take(limit),
            cancellationToken));
    }

    [HttpGet("promotions")]
    [AllowAnonymous]
    public async Task<ActionResult<List<DealDto>>> GetPromotions([FromQuery] string? category = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow.Date;
        var dealsRaw = await _context.Promotions
            .AsNoTracking()
            .Where(p => p.IsActive && (p.ExpiresAt == null || p.ExpiresAt >= now))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var deals = dealsRaw
            .Select(MapDeal)
            .ToList();

        var businessIds = deals
            .Where(d => d.BusinessId.HasValue)
            .Select(d => d.BusinessId!.Value)
            .Distinct()
            .ToList();

        if (businessIds.Count > 0)
        {
            var businessMap = await _context.Businesses
                .AsNoTracking()
                .Where(b => businessIds.Contains(b.Id))
                .Select(b => new { b.Id, b.BusinessName, b.ImageUrl })
                .ToDictionaryAsync(x => x.Id, x => (Name: x.BusinessName, Image: x.ImageUrl), cancellationToken);

            foreach (var deal in deals)
            {
                if (deal.BusinessId.HasValue && businessMap.TryGetValue(deal.BusinessId.Value, out var business))
                {
                    deal.BusinessName = business.Name;
                    deal.BusinessImage = business.Image;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalized = NormalizeDealCategory(category);
            if (normalized is null)
                return BadRequest(new ErrorResponseDto { Message = "Kategoria e deal-it eshte e pavlefshme." });

            deals = deals.Where(d => string.Equals(d.Category, normalized, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Ok(deals);
    }

    [HttpPost("promotions")]
    [Authorize(Roles = "BusinessOwner,Admin")]
    public async Task<ActionResult<DealDto>> CreatePromotion([FromBody] CreateDealRequestDto request, CancellationToken cancellationToken)
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
            return Unauthorized(new ErrorResponseDto { Message = "I paautorizuar." });

        var category = NormalizeDealCategory(request.Category);
        if (category is null)
            return BadRequest(new ErrorResponseDto { Message = "Category duhet te jete Discounts, FlashSales ose EarlyAccess." });

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new ErrorResponseDto { Message = "Titulli eshte i detyrueshem." });

        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new ErrorResponseDto { Message = "Pershkrimi eshte i detyrueshem." });

        if (request.OriginalPrice is < 0 || request.DiscountedPrice is < 0)
            return BadRequest(new ErrorResponseDto { Message = "Cmimet nuk mund te jene negative." });

        if (request.OriginalPrice is not null && request.DiscountedPrice is not null &&
            request.DiscountedPrice > request.OriginalPrice)
            return BadRequest(new ErrorResponseDto { Message = "DiscountedPrice nuk mund te jete me i larte se OriginalPrice." });

        var userRole = User.FindFirstValue(ClaimTypes.Role);
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? ownerId = Guid.TryParse(userIdValue, out var parsedUserId) ? parsedUserId : null;

        Guid? businessId = request.BusinessId;
        if (string.Equals(userRole, "BusinessOwner", StringComparison.OrdinalIgnoreCase))
        {
            if (ownerId is null)
                return Unauthorized(new ErrorResponseDto { Message = "Perdoruesi nuk u identifikua." });

            var ownedBusinessIds = await _context.Businesses
                .AsNoTracking()
                .Where(b => b.OwnerId == ownerId.Value)
                .Select(b => b.Id)
                .ToListAsync(cancellationToken);

            if (ownedBusinessIds.Count == 0)
                return BadRequest(new ErrorResponseDto { Message = "Nuk ke biznes te regjistruar per kete deal." });

            if (businessId is null || businessId == Guid.Empty)
                businessId = ownedBusinessIds[0];
            else if (!ownedBusinessIds.Contains(businessId.Value))
                return Forbid();
        }

        var entity = new Promotion
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            Title = request.Title.Trim(),
            Description = EncodeDealDescription(
                request.Description.Trim(),
                category,
                request.OriginalPrice,
                request.DiscountedPrice),
            ExpiresAt = request.ExpiresAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Promotions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Created($"/promotions/{entity.Id}", MapDeal(entity));
    }

    [HttpGet("reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ReviewDto>>> GetReviews([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        var reviews = await _context.Comments
            .AsNoTracking()
            .Where(c => c.Business.Status == BusinessStatus.Approved)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .Select(c => new ReviewDto
            {
                Id = c.Id,
                ReviewerName = c.User.Username,
                Rating = c.Rate,
                Comment = c.Text
            })
            .ToListAsync(cancellationToken);

        return Ok(reviews);
    }

    [HttpPost("subscribe")]
    [AllowAnonymous]
    [EnableRateLimiting("subscribe")]
    public async Task<ActionResult<MessageResponseDto>> Subscribe([FromBody] SubscribeRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new ErrorResponseDto { Message = "Email eshte i detyrueshem." });

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(normalizedEmail))
            return BadRequest(new ErrorResponseDto { Message = "Email i pavlefshem." });

        var exists = await _context.NewsletterSubscribers.AnyAsync(s => s.Email == normalizedEmail, cancellationToken);
        if (!exists)
        {
            _context.NewsletterSubscribers.Add(new NewsletterSubscriber
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Ok(new MessageResponseDto { Message = "Success" });
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<List<PublicBusinessDto>>> Search([FromQuery] SearchQueryDto query, CancellationToken cancellationToken)
    {
        query.Limit = Math.Clamp(query.Limit, 1, 100);
        query.Page = Math.Max(1, query.Page);

        if (query.Lat is < -90 or > 90 || query.Lng is < -180 or > 180)
            return BadRequest(new ErrorResponseDto { Message = "Lat/Lng jashte kufijve." });

        if (query.RadiusKm is < 0.1 or > 300)
            return BadRequest(new ErrorResponseDto { Message = "RadiusKm duhet te jete 0.1 - 300." });

        var businesses = _context.Businesses
            .AsNoTracking()
            .Where(b => b.Status == BusinessStatus.Approved)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = NormalizeSearchKeyword(query.Keyword);
            var matchesBusinessType = TryMapKeywordToBusinessType(keyword, out var keywordBusinessType);
            businesses = businesses.Where(b =>
                (b.BusinessName != null && b.BusinessName.ToLower().Contains(keyword)) ||
                (b.Description != null && b.Description.ToLower().Contains(keyword)) ||
                (b.City != null && b.City.ToLower().Contains(keyword)) ||
                (b.Address != null && b.Address.ToLower().Contains(keyword)) ||
                (matchesBusinessType && b.BusinessType == keywordBusinessType));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var categoryTypes = query.Category
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(c => Enum.TryParse<BusinessType>(c, true, out var businessType)
                    ? businessType
                    : (BusinessType?)null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();

            if (categoryTypes.Count == 0)
                return Ok(new List<PublicBusinessDto>());

            businesses = businesses.Where(b => categoryTypes.Contains(b.BusinessType));
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var locations = ParseLocations(query.Location);
            businesses = businesses.Where(b => locations.Contains(b.City.ToLower()));
        }

        if (query.OnlyWithCoordinates)
        {
            businesses = businesses.Where(HasValidCoordinatesExpression());
        }

        var bbox = ParseBbox(query.Bbox);
        if (bbox is not null)
        {
            businesses = businesses.Where(b =>
                b.Longitude >= (decimal)bbox.Value.MinLng &&
                b.Longitude <= (decimal)bbox.Value.MaxLng &&
                b.Latitude >= (decimal)bbox.Value.MinLat &&
                b.Latitude <= (decimal)bbox.Value.MaxLat);
        }

        if (string.Equals(query.SortBy, "createdAt", StringComparison.OrdinalIgnoreCase) &&
            query.Lat is null &&
            query.Lng is null)
        {
            businesses = businesses.OrderByDescending(b => b.CreatedAt);
        }

        var projected = await businesses
            .Select(b => new PublicBusinessDto
            {
                Id = b.Id,
                Name = b.BusinessName,
                Logo = b.ImageUrl,
                Description = b.Description,
                Phone = b.PhoneNumber,
                Email = b.Email,
                Category = b.BusinessType.ToString(),
                Location = b.City,
                Featured = b.Featured,
                Coordinates = b.Latitude != null && b.Longitude != null
                    ? new CoordinatesDto
                    {
                        Lat = (double)b.Latitude.Value,
                        Lng = (double)b.Longitude.Value
                    }
                    : null
            })
            .ToListAsync(cancellationToken);

        var ratings = await _context.Comments
            .AsNoTracking()
            .Where(c => projected.Select(p => p.Id).Contains(c.BusinessId))
            .GroupBy(c => c.BusinessId)
            .Select(g => new { BusinessId = g.Key, Rating = g.Average(x => (double)x.Rate) })
            .ToListAsync(cancellationToken);

        var ratingMap = ratings.ToDictionary(x => x.BusinessId, x => x.Rating);
        foreach (var item in projected)
        {
            item.Rating = ratingMap.GetValueOrDefault(item.Id, 0);
            if (item.Coordinates is null &&
                TryGetCityCoordinates(item.Location, out var inferredCoordinates))
            {
                item.Coordinates = inferredCoordinates;
            }
        }

        if (query.Lat is not null && query.Lng is not null)
        {
            projected = projected
                .Where(x => x.Coordinates is not null)
                .Select(x =>
                {
                    x.Distance = HaversineMeters(query.Lat.Value, query.Lng.Value, x.Coordinates!.Lat, x.Coordinates.Lng);
                    return x;
                })
                .Where(x => query.RadiusKm is null || x.Distance <= query.RadiusKm * 1000)
                .OrderBy(x => x.Distance)
                .ToList();
        }
        else if (string.Equals(query.SortBy, "rating", StringComparison.OrdinalIgnoreCase))
        {
            projected = projected.OrderByDescending(x => x.Rating).ToList();
        }

        projected = projected
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToList();

        return Ok(projected);
    }

    [HttpGet("businesses/{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicBusinessDetailDto>> GetBusinessDetails(Guid id, CancellationToken cancellationToken)
    {
        var item = await _context.Businesses
            .AsNoTracking()
            .Where(b => b.Id == id && b.Status == BusinessStatus.Approved)
            .Select(b => new PublicBusinessDetailDto
            {
                Id = b.Id,
                Name = b.BusinessName,
                Description = b.Description,
                Category = b.BusinessType.ToString(),
                Address = b.Address,
                Location = b.City,
                Phone = b.PhoneNumber,
                Email = b.Email,
                Logo = b.ImageUrl,
                Coordinates = b.Latitude != null && b.Longitude != null
                    ? new CoordinatesDto
                    {
                        Lat = (double)b.Latitude.Value,
                        Lng = (double)b.Longitude.Value
                    }
                    : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
            return NotFound(new ErrorResponseDto { Message = "Biznesi nuk u gjet." });

        var reviewStats = await _context.Comments
            .AsNoTracking()
            .Where(c => c.BusinessId == id)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Rating = g.Average(x => (double)x.Rate)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (reviewStats is not null)
        {
            item.ReviewsCount = reviewStats.Count;
            item.Rating = reviewStats.Rating;
        }

        if (item.Coordinates is null &&
            TryGetCityCoordinates(item.Location, out var inferredCoordinates))
        {
            item.Coordinates = inferredCoordinates;
        }

        return Ok(item);
    }

    [HttpGet("opendays")]
    [AllowAnonymous]
    public async Task<ActionResult<BusinessOpenDaysDto>> GetOpenDays([FromQuery] Guid businessId, CancellationToken cancellationToken)
    {
        var business = await _context.Businesses
            .AsNoTracking()
            .Where(b => b.Id == businessId && b.Status == BusinessStatus.Approved)
            .Select(b => new { b.Id, b.OpenDaysMask })
            .FirstOrDefaultAsync(cancellationToken);

        if (business is null)
            return NotFound(new ErrorResponseDto { Message = "Biznesi nuk u gjet." });

        return Ok(MapOpenDaysDto(business.Id, business.OpenDaysMask));
    }

    private async Task<List<PublicBusinessDto>> ProjectPublicBusinesses(IQueryable<Business> query, CancellationToken cancellationToken)
    {
        var list = await query
            .Select(b => new PublicBusinessDto
            {
                Id = b.Id,
                Name = b.BusinessName,
                Logo = b.ImageUrl,
                Description = b.Description,
                Phone = b.PhoneNumber,
                Email = b.Email,
                Category = b.BusinessType.ToString(),
                Location = b.City,
                Featured = b.Featured,
                Coordinates = b.Latitude != null && b.Longitude != null
                    ? new CoordinatesDto
                    {
                        Lat = (double)b.Latitude.Value,
                        Lng = (double)b.Longitude.Value
                    }
                    : null
            })
            .ToListAsync(cancellationToken);

        var ids = list.Select(x => x.Id).ToList();
        var ratings = await _context.Comments
            .AsNoTracking()
            .Where(c => ids.Contains(c.BusinessId))
            .GroupBy(c => c.BusinessId)
            .Select(g => new { BusinessId = g.Key, Rating = g.Average(x => (double)x.Rate) })
            .ToDictionaryAsync(x => x.BusinessId, x => x.Rating, cancellationToken);

        foreach (var item in list)
        {
            item.Rating = ratings.GetValueOrDefault(item.Id, 0);
            if (item.Coordinates is null &&
                TryGetCityCoordinates(item.Location, out var inferredCoordinates))
            {
                item.Coordinates = inferredCoordinates;
            }
        }

        return list;
    }

    private static (double MinLng, double MinLat, double MaxLng, double MaxLat)? ParseBbox(string? bbox)
    {
        if (string.IsNullOrWhiteSpace(bbox))
            return null;

        var parts = bbox.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 4)
            return null;

        if (!double.TryParse(parts[0], out var minLng) ||
            !double.TryParse(parts[1], out var minLat) ||
            !double.TryParse(parts[2], out var maxLng) ||
            !double.TryParse(parts[3], out var maxLat))
            return null;

        return (minLng, minLat, maxLng, maxLat);
    }

    private static List<BusinessType> ParseBusinessTypes(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return new List<BusinessType>();

        return category
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(c => Enum.TryParse<BusinessType>(c, true, out var businessType)
                ? businessType
                : (BusinessType?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
    }

    private static List<string> ParseLocations(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return new List<string>();

        return location
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(ExpandLocationAliases)
            .Distinct()
            .ToList();
    }

    private static string NormalizeSearchKeyword(string rawKeyword)
    {
        var keyword = rawKeyword.Trim().ToLowerInvariant();
        keyword = keyword.Replace("near me", string.Empty, StringComparison.OrdinalIgnoreCase);
        keyword = keyword.Replace("afër meje", string.Empty, StringComparison.OrdinalIgnoreCase);
        keyword = keyword.Replace("afer meje", string.Empty, StringComparison.OrdinalIgnoreCase);
        keyword = keyword.Replace("prane meje", string.Empty, StringComparison.OrdinalIgnoreCase);

        while (keyword.Contains("  ", StringComparison.Ordinal))
            keyword = keyword.Replace("  ", " ", StringComparison.Ordinal);

        return keyword.Trim();
    }

    private static bool TryMapKeywordToBusinessType(string keyword, out BusinessType businessType)
    {
        businessType = keyword switch
        {
            "restaurant" or "restorant" => BusinessType.Restaurant,
            "cafe" or "coffee" or "coffee shop" => BusinessType.Cafe,
            "shop" or "store" or "market" => BusinessType.Shop,
            "car wash" or "pharmacy" or "service" => BusinessType.Service,
            _ => BusinessType.Unknown
        };

        return businessType != BusinessType.Unknown;
    }

    private static IEnumerable<string> ExpandLocationAliases(string rawLocation)
    {
        if (string.IsNullOrWhiteSpace(rawLocation))
            return Enumerable.Empty<string>();

        var token = rawLocation.Trim().ToLowerInvariant();
        var normalized = token
            .Replace('ë', 'e')
            .Replace('ç', 'c');

        return normalized switch
        {
            "prishtine" or "prishtina" => new[] { "prishtine", "prishtinë", "prishtina" },
            "mitrovice" or "mitrovica" => new[] { "mitrovice", "mitrovicë", "mitrovica" },
            _ => new[] { token }
        };
    }

    private static bool TryGetCityCoordinates(string? city, out CoordinatesDto? coordinates)
    {
        coordinates = null;
        if (string.IsNullOrWhiteSpace(city))
            return false;

        return KosovoCityCoordinates.TryGetValue(city.Trim(), out coordinates);
    }

    internal static BusinessOpenDaysDto MapOpenDaysDto(Guid businessId, int mask)
    {
        return new BusinessOpenDaysDto
        {
            BusinessId = businessId,
            MondayOpen = IsDayOpen(mask, 0),
            TuesdayOpen = IsDayOpen(mask, 1),
            WednesdayOpen = IsDayOpen(mask, 2),
            ThursdayOpen = IsDayOpen(mask, 3),
            FridayOpen = IsDayOpen(mask, 4),
            SaturdayOpen = IsDayOpen(mask, 5),
            SundayOpen = IsDayOpen(mask, 6),
        };
    }

    internal static int BuildOpenDaysMask(UpdateOpenDaysRequestDto request)
    {
        var mask = 0;
        if (request.MondayOpen) mask |= (1 << 0);
        if (request.TuesdayOpen) mask |= (1 << 1);
        if (request.WednesdayOpen) mask |= (1 << 2);
        if (request.ThursdayOpen) mask |= (1 << 3);
        if (request.FridayOpen) mask |= (1 << 4);
        if (request.SaturdayOpen) mask |= (1 << 5);
        if (request.SundayOpen) mask |= (1 << 6);
        return mask;
    }

    private static bool IsDayOpen(int mask, int bit) => (mask & (1 << bit)) != 0;

    private static DealDto MapDeal(Promotion promotion)
    {
        var meta = ParseDealMetadata(promotion.Description);
        var description = meta?.Description ?? promotion.Description;
        var category = NormalizeDealCategory(meta?.Category) ?? "Discounts";
        var originalPrice = meta?.OriginalPrice;
        var discountedPrice = meta?.DiscountedPrice;
        var discountPercent = ComputeDiscountPercent(originalPrice, discountedPrice);

        return new DealDto
        {
            Id = promotion.Id,
            BusinessId = promotion.BusinessId,
            BusinessName = promotion.Business?.BusinessName,
            BusinessImage = promotion.Business?.ImageUrl,
            Title = promotion.Title,
            Description = description,
            Category = category,
            OriginalPrice = originalPrice,
            DiscountedPrice = discountedPrice,
            DiscountPercent = discountPercent,
            ExpiresAt = promotion.ExpiresAt
        };
    }

    private static string? NormalizeDealCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return null;

        return category.Trim().ToLowerInvariant() switch
        {
            "discounts" => "Discounts",
            "flashsales" => "FlashSales",
            "flash sales" => "FlashSales",
            "earlyaccess" => "EarlyAccess",
            "early access" => "EarlyAccess",
            _ => null
        };
    }

    private static int? ComputeDiscountPercent(decimal? originalPrice, decimal? discountedPrice)
    {
        if (originalPrice is null || discountedPrice is null || originalPrice <= 0 || discountedPrice > originalPrice)
            return null;

        var value = (int)Math.Round(((originalPrice.Value - discountedPrice.Value) / originalPrice.Value) * 100m);
        return Math.Clamp(value, 0, 100);
    }

    private static string EncodeDealDescription(string description, string category, decimal? originalPrice, decimal? discountedPrice)
    {
        var meta = new DealMetadata
        {
            Category = category,
            Description = description,
            OriginalPrice = originalPrice,
            DiscountedPrice = discountedPrice
        };
        return DealMetaPrefix + JsonSerializer.Serialize(meta);
    }

    private static DealMetadata? ParseDealMetadata(string? source)
    {
        if (string.IsNullOrWhiteSpace(source) || !source.StartsWith(DealMetaPrefix, StringComparison.Ordinal))
            return null;

        var payload = source.Substring(DealMetaPrefix.Length);
        try
        {
            return JsonSerializer.Deserialize<DealMetadata>(payload);
        }
        catch
        {
            return null;
        }
    }

    private sealed class DealMetadata
    {
        public string Category { get; set; } = "Discounts";
        public string Description { get; set; } = string.Empty;
        public decimal? OriginalPrice { get; set; }
        public decimal? DiscountedPrice { get; set; }
    }

    private static System.Linq.Expressions.Expression<Func<Business, bool>> HasValidCoordinatesExpression()
    {
        return b => b.Latitude != null &&
                    b.Longitude != null &&
                    !(b.Latitude == 0 && b.Longitude == 0);
    }

    private static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double radius = 6371000;
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return radius * c;
    }

    private static double ToRadians(double value) => value * (Math.PI / 180.0);
}

