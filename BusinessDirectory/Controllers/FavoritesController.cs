using System.Security.Claims;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public FavoritesController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    [HttpPost("{businessId:guid}")]
    public async Task<IActionResult> Toggle(Guid businessId, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var (isFavorite, notFound) = await _favoriteService.ToggleAsync(userId.Value, businessId, ct);
        
        if (notFound)
            return NotFound(new { message = "Biznesi nuk u gjet." });

        return Ok(new { isFavorite });
    }

    [HttpGet]
    public async Task<IActionResult> GetMyFavorites(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var favorites = await _favoriteService.GetUserFavoritesAsync(userId.Value, ct);
        return Ok(favorites);
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
