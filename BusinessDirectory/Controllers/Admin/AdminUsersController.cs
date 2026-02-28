using BusinessDirectory.Controllers;
using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Dtos.User;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.API.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _userService;

    public AdminUsersController(IAdminUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPatch("{id:guid}/role")]
    public async Task<IActionResult> UpdateRoleAsync(Guid id, [FromBody] UserRoleUpdateDto dto, CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId();
        if (actorUserId is null)
            return Unauthorized();

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        var (notFound, forbid, error, result) = await _userService.UpdateRoleAsync(
            actorUserId.Value,
            id,
            dto.Role,
            dto.Reason,
            ipAddress,
            userAgent,
            cancellationToken);

        if (notFound) return NotFound();
        if (forbid) return Forbid();
        if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUserAsync(
        Guid id,
        [FromQuery] string? reason,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId();
        if (actorUserId is null)
            return Unauthorized();

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        var (notFound, forbid, error) = await _userService.DeleteUserAsync(
            actorUserId.Value,
            id,
            reason,
            ipAddress,
            userAgent,
            cancellationToken);

        if (notFound) return NotFound();
        if (forbid) return Forbid();
        if (!string.IsNullOrWhiteSpace(error)) return BadRequest(new { message = error });
        return NoContent();
    }

    private Guid? GetUserId()
    {
        return User.GetActorUserId();
    }
}
