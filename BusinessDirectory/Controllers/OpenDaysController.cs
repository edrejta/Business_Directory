using System.Security.Claims;
using BusinessDirectory.Application.Dtos.OpenDays;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("api")]
public sealed class OpenDaysController : ControllerBase
{
    private readonly IOpenDaysService _service;

    public OpenDaysController(IOpenDaysService service)
    {
        _service = service;
    }

    [HttpGet("opendays")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublic([FromQuery] GetOpenDaysQueryDto query, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await _service.GetPublicAsync(query.BusinessId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("owner/opendays")]
    [Authorize(Roles = "BusinessOwner")]
    public async Task<IActionResult> GetOwner([FromQuery] GetOpenDaysQueryDto query, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var (result, notFound, forbid) = await _service.GetOwnerAsync(query.BusinessId, userId.Value, ct);
        if (notFound) return NotFound();
        if (forbid) return Forbid();

        return Ok(result);
    }

    [HttpPost("owner/opendays")]
    [Authorize(Roles = "BusinessOwner")]
    public async Task<IActionResult> SetOwner([FromBody] OwnerUpdateOpenDaysRequestDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var (result, notFound, forbid) = await _service.SetOwnerAsync(userId.Value, request, ct);
        if (notFound) return NotFound();
        if (forbid) return Forbid();

        return Ok(result);
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
