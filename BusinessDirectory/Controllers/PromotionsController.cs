using System.Security.Claims;
using BusinessDirectory.Application.Dtos.Promotions;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("api/promotions")]
public sealed class PromotionsController : ControllerBase
{
    private readonly IPromotionService _service;

    public PromotionsController(IPromotionService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromQuery] GetPromotionsQueryDto query, CancellationToken ct)
    {
        var result = await _service.GetAsync(query, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "BusinessOwner")]
    public async Task<IActionResult> Create([FromBody] CreatePromotionRequestDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var (result, notFound, forbid, error) = await _service.CreateAsync(userId.Value, request, ct);
        if (notFound) return NotFound();
        if (forbid) return Forbid();
        if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });

        return CreatedAtAction(nameof(Get), new { businessId = result!.BusinessId }, result);
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
