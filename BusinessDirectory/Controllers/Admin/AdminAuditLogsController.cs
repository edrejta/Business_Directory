using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.API.Controllers.Admin;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = "Admin")]
public sealed class AdminAuditLogsController : ControllerBase
{
    private readonly IAdminUserService _userService;

    public AdminAuditLogsController(IAdminUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminAuditLogDto>>> GetAuditLogs(
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var logs = await _userService.GetAuditLogsAsync(take, cancellationToken);
        return Ok(logs);
    }
}
