using System.Security.Claims;
using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Entities;
using BusinessDirectory.Domain.Enums;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BusinessesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IBusinessService _businessService;

    public BusinessesController(ApplicationDbContext context, IBusinessService businessService)
    {
        _context = context;
        _businessService = businessService;
    }

    // GET /businesses?search=&city=&type=
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BusinessDto>>> GetBusinesses(
        [FromQuery] string? search,
        [FromQuery] string? city,
        [FromQuery] BusinessType? type,
        CancellationToken cancellationToken)
    {
        var query = _context.Businesses.AsNoTracking()
            .Where(b => b.Status == BusinessStatus.Approved);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(b =>
                b.BusinessName.ToLower().Contains(s) ||
                b.Description.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var c = city.Trim().ToLowerInvariant();
            query = query.Where(b => b.City.ToLower() == c);
        }

        if (type.HasValue && type.Value != BusinessType.Unknown)
        {
            query = query.Where(b => b.BusinessType == type.Value);
        }

        var results = await query
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BusinessDto
            {
                Id = b.Id,
                OwnerId = b.OwnerId,
                BusinessName = b.BusinessName,
                Address = b.Address,
                City = b.City,
                Email = b.Email,
                PhoneNumber = b.PhoneNumber,
                BusinessType = b.BusinessType,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                Status = b.Status,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(results);
    }

    // GET /businesses/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BusinessDto>> GetBusinessById(Guid id, CancellationToken cancellationToken)
    {
        var business = await _context.Businesses.AsNoTracking()
            .Where(b => b.Id == id && b.Status == BusinessStatus.Approved)
            .Select(b => new BusinessDto
            {
                Id = b.Id,
                OwnerId = b.OwnerId,
                BusinessName = b.BusinessName,
                Address = b.Address,
                City = b.City,
                Email = b.Email,
                PhoneNumber = b.PhoneNumber,
                BusinessType = b.BusinessType,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                Status = b.Status,
                CreatedAt = b.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return business is null ? NotFound() : Ok(business);
    }

    // POST /businesses (owner)
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BusinessDto>> CreateBusiness([FromBody] BusinessCreateDto dto, CancellationToken cancellationToken)
    {
        var ownerId = GetUserId();
        if (ownerId is null)
            return Unauthorized();

        var business = new Business
        {
            OwnerId = ownerId.Value,
            BusinessName = dto.BusinessName.Trim(),
            Address = dto.Address.Trim(),
            City = dto.City.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            BusinessType = dto.BusinessType,
            Description = dto.Description.Trim(),
            ImageUrl = dto.ImageUrl.Trim(),
            Status = BusinessStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Businesses.Add(business);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new BusinessDto
        {
            Id = business.Id,
            OwnerId = business.OwnerId,
            BusinessName = business.BusinessName,
            Address = business.Address,
            City = business.City,
            Email = business.Email,
            PhoneNumber = business.PhoneNumber,
            BusinessType = business.BusinessType,
            Description = business.Description,
            ImageUrl = business.ImageUrl,
            Status = business.Status,
            CreatedAt = business.CreatedAt
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    // PUT /businesses/{id} (owner)
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<BusinessDto>> UpdateBusiness(Guid id, [FromBody] BusinessUpdateDto dto, CancellationToken cancellationToken)
    {
        var ownerId = GetUserId();
        if (ownerId is null)
            return Unauthorized();

        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (business is null)
            return NotFound();

        if (business.OwnerId != ownerId.Value)
            return Forbid();

        if (business.Status is not (BusinessStatus.Pending or BusinessStatus.Rejected))
            return BadRequest(new { message = "Business mund të përditësohet vetëm kur është Pending ose Rejected." });

        business.BusinessName = dto.BusinessName.Trim();
        business.Address = dto.Address.Trim();
        business.City = dto.City.Trim();
        business.Email = dto.Email.Trim().ToLowerInvariant();
        business.PhoneNumber = dto.PhoneNumber.Trim();
        business.BusinessType = dto.BusinessType;
        business.Description = dto.Description.Trim();
        business.ImageUrl = dto.ImageUrl.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        var response = new BusinessDto
        {
            Id = business.Id,
            OwnerId = business.OwnerId,
            BusinessName = business.BusinessName,
            Address = business.Address,
            City = business.City,
            Email = business.Email,
            PhoneNumber = business.PhoneNumber,
            BusinessType = business.BusinessType,
            Description = business.Description,
            ImageUrl = business.ImageUrl,
            Status = business.Status,
            CreatedAt = business.CreatedAt
        };

        return Ok(response);
    }

    
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteBusiness(Guid id, CancellationToken cancellationToken)
    {
        var ownerId = GetUserId();
        if (ownerId is null)
            return Unauthorized();

        var result = await _businessService.DeleteAsync(id, ownerId.Value, cancellationToken);

        if (result.NotFound) return NotFound();
        if (result.Forbid) return Forbid();
        if (result.Error is not null) return BadRequest(new { message = result.Error });

        return NoContent();
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
