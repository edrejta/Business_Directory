using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.API.Controllers
{
    [ApiController]
    public class HealthController : ControllerBase
    {
        // fix#3: Keep health check public so uptime probes do not need JWT tokens.
        [AllowAnonymous]
        [HttpGet("/health")]
        [HttpGet("/api/health")]
        public IActionResult Health()
        {
            return Ok(new { status = "ok" });
        }
    }
}
