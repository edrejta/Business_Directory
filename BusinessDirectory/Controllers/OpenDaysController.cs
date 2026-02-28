using BusinessDirectory.Application.Dtos.OpenDays;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OpenDaysController : ControllerBase
{
    private readonly IOpenDaysService _service;

    public OpenDaysController(IOpenDaysService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublic([FromQuery] GetOpenDaysQueryDto query, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (query.BusinessId == Guid.Empty)
            return BadRequest(new { message = "BusinessId eshte i detyrueshem." });

        var result = await _service.GetPublicAsync(query.BusinessId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("owner")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> GetOwner([FromQuery] GetOpenDaysQueryDto query, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (query.BusinessId == Guid.Empty)
            return BadRequest(new { message = "BusinessId eshte i detyrueshem." });

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { message = "Nuk u identifikua perdoruesi." });

        var (result, notFound, forbid) = await _service.GetOwnerAsync(query.BusinessId, userId.Value, ct);

        if (notFound) return NotFound(new { message = "Biznesi nuk u gjet." });
        if (forbid) return StatusCode(403, new { message = "Nuk ke te drejte te shohesh ditet e hapura per kete biznes." });

        return Ok(result);
    }

    [HttpPost("owner")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> SetOwner([FromBody] OwnerUpdateOpenDaysRequestDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new { message = "Nuk u identifikua perdoruesi." });

        if (request.BusinessId == Guid.Empty)
            return BadRequest(new { message = "BusinessId eshte i detyrueshem." });

        var (result, notFound, forbid) = await _service.SetOwnerAsync(userId.Value, request, ct);

        if (notFound) return NotFound(new { message = "Biznesi nuk u gjet." });
        if (forbid) return StatusCode(403, new { message = "Nuk ke te drejte te ndryshosh ditet e hapura per kete biznes." });

        return Ok(result);
    }

    private Guid? GetUserId()
    {
        return User.GetActorUserId();
    }
}
