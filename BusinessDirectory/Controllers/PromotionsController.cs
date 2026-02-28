using BusinessDirectory.Application.Dtos.Promotions;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        var actorUserId = User.GetActorUserId();
        if (actorUserId is null)
            return Unauthorized();

        var (result, notFound, forbid, error) = await _promotions.CreateAsync(actorUserId.Value, request, ct);

        if (notFound) return NotFound();
        if (forbid) return Forbid();
        if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });

        return Ok(result);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "BusinessOwner")]
    public async Task<ActionResult<IReadOnlyList<PromotionResponseDto>>> Mine([FromQuery] Guid? businessId, [FromQuery] string? category, CancellationToken ct)
    {
        var actorUserId = User.GetActorUserId();
        if (actorUserId is null)
            return Unauthorized();

        var result = await _promotions.GetMineAsync(actorUserId.Value, businessId, category, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "BusinessOwner")]
    public async Task<ActionResult<PromotionResponseDto>> Update(Guid id, [FromBody] UpdatePromotionRequestDto request, CancellationToken ct)
    {
        var actorUserId = User.GetActorUserId();
        if (actorUserId is null)
            return Unauthorized();

        var (result, notFound, forbid, error) = await _promotions.UpdateAsync(actorUserId.Value, id, request, ct);

        if (notFound) return NotFound();
        if (forbid) return Forbid();
        if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "BusinessOwner")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var actorUserId = User.GetActorUserId();
        if (actorUserId is null)
            return Unauthorized();

        var (notFound, forbid, error) = await _promotions.DeleteAsync(actorUserId.Value, id, ct);

        if (notFound) return NotFound();
        if (forbid) return Forbid();
        if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });

        return NoContent();
    }

}
