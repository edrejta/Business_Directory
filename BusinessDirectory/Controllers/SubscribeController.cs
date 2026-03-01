using BusinessDirectory.Application.Dtos.Subscribe;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("api/subscribe")]
public sealed class SubscribeController : ControllerBase
{
    private readonly ISubscribeService _service;

    public SubscribeController(ISubscribeService service)
    {
        _service = service;
    }

    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(SubscribeResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SubscribeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDto? request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new { message = "Request body is required." });

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var result = await _service.SubscribeAsync(request, ct);
            return result.Created
                ? StatusCode(StatusCodes.Status201Created, result)
                : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
