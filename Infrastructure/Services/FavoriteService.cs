using BusinessDirectory.Application.Dtos.Businesses;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BusinessDirectory.Infrastructure.Services;

public sealed class FavoriteService : IFavoriteService
{
    private readonly ApplicationDbContext _db;

    public FavoriteService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(bool IsFavorite, bool NotFound)> ToggleAsync(Guid userId, Guid businessId, CancellationToken ct)
    {
        var existing = await _db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.BusinessId == businessId, ct);

        if (existing is not null)
        {
            _db.Favorites.Remove(existing);
            await _db.SaveChangesAsync(ct);
            return (false, false);
        }

        var businessExists = await _db.Businesses.AnyAsync(b => b.Id == businessId, ct);
        if (!businessExists)
            return (false, true);

        _db.Favorites.Add(new Favorite
        {
            UserId = userId,
            BusinessId = businessId
        });

        await _db.SaveChangesAsync(ct);
        return (true, false);
    }

    public async Task<IReadOnlyList<BusinessDto>> GetUserFavoritesAsync(Guid userId, CancellationToken ct)
    {
        var list = await _db.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Join(
                _db.Businesses.AsNoTracking(),
                f => f.BusinessId,
                b => b.Id,
                (f, b) => new BusinessDto
                {
                    Id = b.Id,
                    OwnerId = b.OwnerId,

                    BusinessName = b.BusinessName,
                    Type = b.BusinessType.ToString(),

                    Address = b.Address,
                    City = b.City,
                    PhoneNumber = b.PhoneNumber,
                    Description = b.Description,
                    ImageUrl = b.ImageUrl,

                    BusinessUrl = b.WebsiteUrl,

                    BusinessNumber = b.BusinesssNumber,
                    Email = b.Email,

                    Status = b.Status,
                    CreatedAt = b.CreatedAt,

                    SuspensionReason = b.SuspensionReason,
                    IsFavorite = true
                }
            )
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return list;
    }
}