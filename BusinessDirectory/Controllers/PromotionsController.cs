using BusinessDirectory.Application.Dtos.Promotions;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PromotionsController : ControllerBase
{
    private readonly IPromotionService _promotions;

    public PromotionsController(IPromotionService promotions)
    {
        _promotions = promotions;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PromotionResponseDto>>> Get([FromQuery] GetPromotionsQueryDto query, CancellationToken ct)
    {
        var result = await _promotions.GetAsync(query, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "BusinessOwner")]
    public async Task<ActionResult<PromotionResponseDto>> Create([FromBody] CreatePromotionRequestDto request, CancellationToken ct)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        var (result, notFound, forbid, error) = await _promotions.CreateAsync(actorUserId, request, ct);

        if (notFound) return NotFound();
        if (forbid) return Forbid();
        if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });

        return Ok(result);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "BusinessOwner")]
    public async Task<ActionResult<IReadOnlyList<PromotionResponseDto>>> Mine([FromQuery] Guid? businessId, [FromQuery] string? category, CancellationToken ct)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        var result = await _promotions.GetMineAsync(actorUserId, businessId, category, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "BusinessOwner")]
    public async Task<ActionResult<PromotionResponseDto>> Update(Guid id, [FromBody] UpdatePromotionRequestDto request, CancellationToken ct)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        var (result, notFound, forbid, error) = await _promotions.UpdateAsync(actorUserId, id, request, ct);

        if (notFound) return NotFound();
        if (forbid) return Forbid();
        if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "BusinessOwner")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        var (notFound, forbid, error) = await _promotions.DeleteAsync(actorUserId, id, ct);

        if (notFound) return NotFound();
        if (forbid) return Forbid();
        if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });

        return NoContent();
    }

    private bool TryGetActorUserId(out Guid actorUserId)
    {
        actorUserId = default;

        var raw =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("id");

        return Guid.TryParse(raw, out actorUserId);
    }
}