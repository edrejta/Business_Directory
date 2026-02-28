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
    private readonly ILogger<PromotionsController> _logger;

    public PromotionsController(IPromotionService promotions, ILogger<PromotionsController> logger)
    {
        _promotions = promotions;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PromotionResponseDto>>> Get(
        [FromQuery] GetPromotionsQueryDto query,
        CancellationToken ct)
    {
        try
        {
            var result = await _promotions.GetAsync(query, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get promotions failed.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ndodhi një gabim në server." });
        }
    }

    [HttpPost]
    [Authorize(Roles = "BusinessOwner")]
    public async Task<ActionResult<PromotionResponseDto>> Create(
        [FromBody] CreatePromotionRequestDto request,
        CancellationToken ct)
    {
        var actorUserId = User.GetActorUserId();
        if (actorUserId is null)
            return Unauthorized();

        try
        {
            var (result, notFound, forbid, error) = await _promotions.CreateAsync(actorUserId.Value, request, ct);

            if (notFound) return NotFound();
            if (forbid) return Forbid();
            if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create promotion failed. actorUserId={ActorUserId}", actorUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ndodhi një gabim në server." });
        }
    }

    [HttpGet("mine")]
    [Authorize(Roles = "BusinessOwner")]
    public async Task<ActionResult<IReadOnlyList<PromotionResponseDto>>> Mine(
        [FromQuery] Guid? businessId,
        [FromQuery] string? category,
        CancellationToken ct)
    {
        var actorUserId = User.GetActorUserId();
        if (actorUserId is null)
            return Unauthorized();

        try
        {
            var result = await _promotions.GetMineAsync(actorUserId.Value, businessId, category, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get my promotions failed. actorUserId={ActorUserId}, businessId={BusinessId}, category={Category}", actorUserId, businessId, category);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ndodhi një gabim në server." });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "BusinessOwner")]
    public async Task<ActionResult<PromotionResponseDto>> Update(
        Guid id,
        [FromBody] UpdatePromotionRequestDto request,
        CancellationToken ct)
    {
        var actorUserId = User.GetActorUserId();
        if (actorUserId is null)
            return Unauthorized();

        try
        {
            var (result, notFound, forbid, error) = await _promotions.UpdateAsync(actorUserId.Value, id, request, ct);

            if (notFound) return NotFound();
            if (forbid) return Forbid();
            if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update promotion failed. actorUserId={ActorUserId}, id={PromotionId}", actorUserId, id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ndodhi një gabim në server." });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "BusinessOwner")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var actorUserId = User.GetActorUserId();
        if (actorUserId is null)
            return Unauthorized();

        try
        {
            var (notFound, forbid, error) = await _promotions.DeleteAsync(actorUserId.Value, id, ct);

            if (notFound) return NotFound();
            if (forbid) return Forbid();
            if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete promotion failed. actorUserId={ActorUserId}, id={PromotionId}", actorUserId, id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ndodhi një gabim në server." });
        }
    }
}