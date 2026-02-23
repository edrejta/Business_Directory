using BusinessDirectory.Application.Dtos;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("owner")]
[Authorize(Roles = "BusinessOwner,Admin")]
public sealed class OwnerController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OwnerController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("opendays")]
    public async Task<ActionResult<BusinessOpenDaysDto>> GetOwnerOpenDays([FromQuery] Guid? businessId, CancellationToken cancellationToken)
    {
        var business = await ResolveOwnedBusinessAsync(businessId, cancellationToken);
        if (business is null)
            return NotFound(new ErrorResponseDto { Message = "Biznesi nuk u gjet." });

        return Ok(HomepageController.MapOpenDaysDto(business.Id, business.OpenDaysMask));
    }

    [HttpPut("opendays")]
    public async Task<ActionResult<BusinessOpenDaysDto>> UpdateOwnerOpenDays([FromBody] UpdateOpenDaysRequestDto request, CancellationToken cancellationToken)
    {
        var business = await ResolveOwnedBusinessAsync(request.BusinessId, cancellationToken);
        if (business is null)
            return NotFound(new ErrorResponseDto { Message = "Biznesi nuk u gjet." });

        business.OpenDaysMask = HomepageController.BuildOpenDaysMask(request);
        business.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(HomepageController.MapOpenDaysDto(business.Id, business.OpenDaysMask));
    }

    private async Task<Domain.Entities.Business?> ResolveOwnedBusinessAsync(Guid? businessId, CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) && !isAdmin)
            return null;

        var query = _context.Businesses.AsQueryable();

        if (!isAdmin)
            query = query.Where(b => b.OwnerId == userId);

        if (businessId is not null && businessId != Guid.Empty)
            query = query.Where(b => b.Id == businessId.Value);

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

