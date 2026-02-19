using System.Security.Claims;
using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

 
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var user = await _users.GetMeAsync(userId.Value, ct);
        return user is null ? NotFound() : Ok(user);
    }

 
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }


    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserUpdateDto dto, CancellationToken ct)
    {
        var currentUserId = GetUserId();
        if (currentUserId is null)
            return Unauthorized();

        var (notFound, forbid, error, result) = await _users.UpdateAsync(id, currentUserId.Value, dto, ct);

        if (notFound) return NotFound();
        if (forbid) return Forbid();
        if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });

        return Ok(result);
    }

    [HttpPatch("{id:guid}/role")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UserRoleUpdateDto dto, CancellationToken ct)
    {
        var (notFound, result) = await _users.UpdateRoleAsync(id, dto.Role, ct);
        if (notFound) return NotFound();
        return Ok(result);
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
