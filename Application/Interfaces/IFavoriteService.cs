using BusinessDirectory.Application.Dtos.Businesses;

namespace BusinessDirectory.Application.Interfaces;

public interface IFavoriteService
{

    Task<(bool IsFavorite, bool NotFound)> ToggleAsync(Guid userId, Guid businessId, CancellationToken ct);

    Task<IReadOnlyList<BusinessDto>> GetUserFavoritesAsync(Guid userId, CancellationToken ct);
}
