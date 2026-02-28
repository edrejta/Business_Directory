using BusinessDirectory.Application.Dtos.Businesses;
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
    public async Task<ActionResult<IReadOnlyList<BusinessPublicDto>>> GetPublicApproved(
        [FromQuery] string? search,
        [FromQuery] string? city,
        [FromQuery] BusinessType? type,
        CancellationToken cancellationToken)
    {
        var results = await _businessService.GetApprovedAsync(search, city, type, cancellationToken);

        var publicResults = results.Select(b => new BusinessPublicDto
        {
            Id = b.Id,
            BusinessName = b.BusinessName,
            Description = b.Description,
            City = b.City,
            Address = b.Address,
            Category = b.BusinessType.ToString(),
            PhoneNumber = b.PhoneNumber,
            Email = b.Email,
            OpenDays = b.OpenDays,
            ImageUrl = b.ImageUrl
        }).ToList();

        return Ok(publicResults);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BusinessPublicDto>> GetBusinessById(Guid id, CancellationToken cancellationToken)
    {
        var business = await _businessService.GetApprovedByIdAsync(id, cancellationToken);

        if (business is null)
            return NotFound();

        var dto = new BusinessPublicDto
        {
            Id = business.Id,
            BusinessName = business.BusinessName,
            Description = business.Description,
            City = business.City,
            Address = business.Address,
            Category = business.BusinessType.ToString(),
            PhoneNumber = business.PhoneNumber,
            Email = business.Email,
            OpenDays = business.OpenDays,
            ImageUrl = business.ImageUrl
        };

        return Ok(dto);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<BusinessDto>> CreateBusiness(
        [FromBody] BusinessCreateDto dto,
        CancellationToken cancellationToken)
    {
        var ownerId = GetUserId();
        if (ownerId is null)
            return Unauthorized();

        var response = await _businessService.CreateAsync(dto, ownerId.Value, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BusinessDto>> UpdateBusiness(
        Guid id,
        [FromBody] BusinessUpdateDto dto,
        CancellationToken cancellationToken)
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

    [Authorize]
    [HttpGet("mine")]
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

    [Authorize]
    [HttpDelete("{id:guid}")]
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
        return User.GetActorUserId();
    }
}
