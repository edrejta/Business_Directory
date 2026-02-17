using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("businesses")]
public class BusinessController : ControllerBase
{
    private readonly BusinessDirectory.Application.Interfaces.IBusinessService _service;

    public BusinessController(BusinessDirectory.Application.Interfaces.IBusinessService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] BusinessCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var created = await _service.CreateAsync(dto, userId.Value, ct);

        return Created($"/businesses/{created.Id}", new { id = created.Id });
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
