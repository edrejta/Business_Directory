using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace BusinessDirectory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Health()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";

        return Ok(new
        {
            status = "ok",
            timestamp = DateTime.UtcNow,
            version
        });
    }
}
