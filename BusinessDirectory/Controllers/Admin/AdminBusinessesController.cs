using System.Security.Claims;
using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Dtos.Businesses;
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        [FromQuery] string? reason,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId();
        if (actorUserId is null)
            return Unauthorized();

        if (reason is { Length: > 500 })
            return BadRequest(new { message = "Reason must be at most 500 characters." });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        var (notFound, forbid, conflict, error) = await _businessService.DeleteAsync(
            id,
            actorUserId.Value,
            reason,
            ipAddress,
            userAgent,
            cancellationToken);

        if (notFound)
            return NotFound();
        if (forbid)
            return Forbid();
        if (conflict)
            return Conflict(new { message = error });
        if (error is not null)
            return BadRequest(new { message = error });

        return NoContent();
    }

    [HttpPatch("{id:guid}/suspend")]
    public async Task<ActionResult<BusinessDto>> SuspendAsync(
        Guid id,
        [FromBody] AdminActionReasonDto? dto,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetUserId();
        if (actorUserId is null)
            return Unauthorized();

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        var result = await _businessService.SuspendAsync(
            id,
            actorUserId.Value,
            dto?.Reason,
            ipAddress,
            userAgent,
            cancellationToken);
        if (result.NotFound)
            return NotFound();
        if (result.Conflict)
            return Conflict(new { message = result.Error });
        if (result.Error is not null)
            return BadRequest(new { message = result.Error });

        return Ok(result.Result);
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
