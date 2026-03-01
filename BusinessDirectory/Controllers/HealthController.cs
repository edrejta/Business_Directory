using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace BusinessDirectory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Health()
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health endpoint failed.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ndodhi një gabim në server." });
        }
    }
}