using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.Controllers;

/// <summary>
/// Endpoint-et vetëm për Admin. Përdorues jo-admin marrin 403 Forbidden.
/// Shto këtu: approve/reject biznesesh, menaxhim kategorish, etj.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    /// <summary>
    /// Shembull endpoint vetëm Admin. Pa token → 401; me token User/BusinessOwner → 403; me token Admin → 200.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetAdminInfo()
    {
        return Ok(new { message = "Hyrje e lejuar për Admin.", role = "Admin" });
    }
}
