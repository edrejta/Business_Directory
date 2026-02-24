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
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await _service.SubscribeAsync(request, ct);
        return Ok(result);
    }
}
