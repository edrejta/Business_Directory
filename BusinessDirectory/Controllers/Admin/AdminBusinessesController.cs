using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.API.Controllers.Admin;

[ApiController]
[Route("api/admin/businesses")]
[Authorize(Roles = "Admin")]
public sealed class AdminBusinessesController : ControllerBase
{
    private readonly IAdminBusinessService _businessService;

    public AdminBusinessesController(IAdminBusinessService businessService)
    {
        _businessService = businessService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BusinessDto>>> GetBusinessesAsync(
        [FromQuery] BusinessStatus? status,
        CancellationToken cancellationToken)
    {
        var businesses = await _businessService.GetAllAsync(status, cancellationToken);
        return Ok(businesses);
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<BusinessDto>>> GetPendingBusinessesAsync(CancellationToken cancellationToken)
    {
        var businesses = await _businessService.GetPendingAsync(cancellationToken);
        return Ok(businesses);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BusinessDto>> GetBusinessByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var business = await _businessService.GetByIdAsync(id, cancellationToken);
        return business is null ? NotFound() : Ok(business);
    }

    [HttpPatch("{id:guid}/approve")]
    public async Task<ActionResult<BusinessDto>> ApproveAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _businessService.ApproveAsync(id, cancellationToken);
        if (result.NotFound)
            return NotFound();
        if (result.Conflict)
            return Conflict(new { message = result.Error });
        if (result.Error is not null)
            return BadRequest(new { message = result.Error });

        return Ok(result.Result);
    }

    [HttpPatch("{id:guid}/reject")]
    public async Task<ActionResult<BusinessDto>> RejectAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _businessService.RejectAsync(id, cancellationToken);
        if (result.NotFound)
            return NotFound();
        if (result.Conflict)
            return Conflict(new { message = result.Error });
        if (result.Error is not null)
            return BadRequest(new { message = result.Error });

        return Ok(result.Result);
    }

    [HttpPatch("{id:guid}/suspend")]
    public async Task<ActionResult<BusinessDto>> SuspendAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _businessService.SuspendAsync(id, cancellationToken);
        if (result.NotFound)
            return NotFound();
        if (result.Conflict)
            return Conflict(new { message = result.Error });
        if (result.Error is not null)
            return BadRequest(new { message = result.Error });

        return Ok(result.Result);
    }
}
