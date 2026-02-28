using BusinessDirectory.Domain.Enums;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusinessDirectory.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public sealed class HomepageCompatController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<HomepageCompatController> _logger;

    public HomepageCompatController(ApplicationDbContext db, ILogger<HomepageCompatController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories(CancellationToken ct)
    {
        try
        {
            var categories = await _db.Businesses
                .AsNoTracking()
                .Where(b => b.Status == BusinessStatus.Approved)
                .Select(b => b.BusinessType)
                .Distinct()
                .OrderBy(x => x)
                .Select(x => x.ToString())
                .ToListAsync(ct);

            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCategories failed.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Ndodhi një gabim në server." });
        }
    }
}