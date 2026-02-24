using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Dtos.Businesses;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Entities;
using BusinessDirectory.Domain.Enums;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BusinessDirectory.Infrastructure.Services;

public class FavoriteService : IFavoriteService
{
    private readonly ApplicationDbContext _db;

    public FavoriteService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(bool IsFavorite, bool NotFound)> ToggleAsync(Guid userId, Guid businessId, CancellationToken ct)
    {
        var businessExists = await _db.Businesses.AnyAsync(b => b.Id == businessId, ct);
        if (!businessExists)
            return (false, true);

        var existing = await _db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.BusinessId == businessId, ct);

        if (existing != null)
        {
            _db.Favorites.Remove(existing);
            await _db.SaveChangesAsync(ct);
            return (false, false);
        }

        var favorite = new Favorite
        {
            UserId = userId,
            BusinessId = businessId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Favorites.Add(favorite);
        await _db.SaveChangesAsync(ct);
        return (true, false);
    }

    public async Task<IReadOnlyList<BusinessDto>> GetUserFavoritesAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => new BusinessDto
            {
                Id = f.Business.Id,
                OwnerId = f.Business.OwnerId,
                BusinessName = f.Business.BusinessName,
                Address = f.Business.Address,
                City = f.Business.City,
                Email = f.Business.Email,
                PhoneNumber = f.Business.PhoneNumber,
                BusinessType = f.Business.BusinessType,
                Description = f.Business.Description,
                ImageUrl = f.Business.ImageUrl,
                Status = f.Business.Status,
                CreatedAt = f.Business.CreatedAt,
                BusinessNumber = f.Business.BusinesssNumber,
                IsFavorite = true
            })
            .ToListAsync(ct);
    }
}
