using System.Security.Claims;
using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BusinessesController : ControllerBase
{
    private readonly IBusinessService _businessService;

    public BusinessesController(IBusinessService businessService)
    {
        _businessService = businessService;
    }

    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<ActionResult<IReadOnlyList<BusinessPublicDto>>> GetPublicApproved(CancellationToken cancellationToken)
    {
        var results = await _businessService.GetApprovedAsync(null, null, null, cancellationToken);

        var publicResults = results.Select(b => new BusinessPublicDto
        {
            Id = b.Id,
            BusinessName = b.BusinessName,
            Description = b.Description,
            City = b.City,
            Category = b.BusinessType.ToString()
        }).ToList();

        return Ok(publicResults);
    }

    
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BusinessDto>>> GetBusinesses(
        [FromQuery] string? search,
        [FromQuery] string? city,
        [FromQuery] BusinessType? type,
        CancellationToken cancellationToken)
    {
        var results = await _businessService.GetApprovedAsync(search, city, type, cancellationToken);
        return Ok(results);
    }

    
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BusinessDto>> GetBusinessById(Guid id, CancellationToken cancellationToken)
    {
        var business = await _businessService.GetApprovedByIdAsync(id, cancellationToken);
        return business is null ? NotFound() : Ok(business);
    }


    [HttpPost]
    [Authorize]
    public async Task<ActionResult<BusinessDto>> CreateBusiness([FromBody] BusinessCreateDto dto, CancellationToken cancellationToken)
    {
        var ownerId = GetUserId();
        if (ownerId is null)
            return Unauthorized();

        var response = await _businessService.CreateAsync(dto, ownerId.Value, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<BusinessDto>> UpdateBusiness(Guid id, [FromBody] BusinessUpdateDto dto, CancellationToken cancellationToken)
    {
        var ownerId = GetUserId();
        if (ownerId is null)
            return Unauthorized();

        var result = await _businessService.UpdateAsync(id, dto, ownerId.Value, cancellationToken);

        if (result.NotFound)
            return NotFound();

        if (result.Forbid)
            return Forbid();

        if (result.Error is not null)
            return BadRequest(new { message = result.Error });

        return Ok(result.Result);
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<BusinessDto>>> GetMine(
        [FromQuery] BusinessStatus? status,
        CancellationToken cancellationToken)
    {
        var ownerId = GetUserId();
        if (ownerId is null)
            return Unauthorized();

        var results = await _businessService.GetMineAsync(ownerId.Value, status, cancellationToken);
        return Ok(results);
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
