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

    public HomepageCompatController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories(CancellationToken ct)
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

}
