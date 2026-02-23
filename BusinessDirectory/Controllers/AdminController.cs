using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Domain.Entities;
using BusinessDirectory.Domain.Enums;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BusinessDirectory.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard(CancellationToken cancellationToken)
    {
        var metrics = new DashboardMetricsDto
        {
            TotalBusinesses = await _context.Businesses.CountAsync(cancellationToken),
            PendingBusinesses = await _context.Businesses.CountAsync(b => b.Status == BusinessStatus.Pending, cancellationToken),
            ApprovedBusinesses = await _context.Businesses.CountAsync(b => b.Status == BusinessStatus.Approved, cancellationToken),
            TotalUsers = await _context.Users.CountAsync(cancellationToken)
        };

        var recentActivityRaw = await _context.Businesses
            .GroupBy(b => b.Status)
            .Select(g => new DashboardActivityDto
            {
                Label = ((int)g.Key).ToString(),
                Value = g.Count()
            })
            .ToListAsync(cancellationToken);

        var recentActivity = recentActivityRaw
            .Select(x => new DashboardActivityDto
            {
                Label = Enum.GetName(typeof(BusinessStatus), int.Parse(x.Label)) ?? x.Label,
                Value = x.Value
            })
            .ToList();

        return Ok(new AdminDashboardDto
        {
            Metrics = metrics,
            RecentActivity = recentActivity
        });
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto
            {
                Id = u.Id.ToString(),
                Username = u.Username,
                FullName = u.Username,
                Email = u.Email,
                Role = (int)u.Role,
                CreatedAt = u.CreatedAt,
                Status = "Active"
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpGet("reports/summary")]
    public async Task<ActionResult<ReportSummaryDto>> GetReportsSummary(CancellationToken cancellationToken)
    {
        var totalReports = await _context.Reports.CountAsync(cancellationToken);
        var openReports = await _context.Reports.CountAsync(r => r.Status == "Open", cancellationToken);
        var resolvedReports = await _context.Reports.CountAsync(r => r.Status == "Resolved", cancellationToken);
        var flaggedBusinesses = await _context.Reports.Select(r => r.BusinessId).Distinct().CountAsync(cancellationToken);

        var byReason = await _context.Reports
            .GroupBy(r => r.Reason)
            .Select(g => new ReportsByReasonDto
            {
                Reason = g.Key,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        return Ok(new ReportSummaryDto
        {
            TotalReports = totalReports,
            OpenReports = openReports,
            ResolvedReports = resolvedReports,
            FlaggedBusinesses = flaggedBusinesses,
            ReportsByReason = byReason
        });
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<AdminCategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        var categoriesRaw = await _context.Businesses
            .GroupBy(b => b.BusinessType)
            .Select(g => new AdminCategoryDto
            {
                Id = ((int)g.Key).ToString(),
                Name = string.Empty,
                BusinessesCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        var categories = categoriesRaw
            .Select(c => new AdminCategoryDto
            {
                Id = c.Id,
                Name = int.TryParse(c.Id, out var parsedType)
                    ? Enum.GetName(typeof(BusinessType), parsedType) ?? c.Id
                    : c.Id,
                BusinessesCount = c.BusinessesCount
            })
            .OrderBy(c => c.Name)
            .ToList();

        return Ok(categories);
    }

    [HttpGet("businesses")]
    public async Task<ActionResult<List<AdminBusinessDto>>> GetBusinesses([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Businesses.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<BusinessStatus>(status, true, out var parsedStatus))
                return BadRequest(new ErrorResponseDto { Message = "Status filter i pavlefshem." });

            query = query.Where(b => b.Status == parsedStatus);
        }

        var entities = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(entities.Select(ToAdminBusinessDto).ToList());
    }

    [HttpGet("businesses/pending")]
    public async Task<ActionResult<List<AdminBusinessDto>>> GetPendingBusinesses(CancellationToken cancellationToken)
    {
        var entities = await _context.Businesses
            .AsNoTracking()
            .Where(b => b.Status == BusinessStatus.Pending)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(entities.Select(ToAdminBusinessDto).ToList());
    }

    [HttpGet("businesses/{id}")]
    public async Task<ActionResult<AdminBusinessDto>> GetBusinessById(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var businessId))
            return NotFound(new ErrorResponseDto { Message = "Biznesi nuk u gjet." });

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

        if (business is null)
            return NotFound(new ErrorResponseDto { Message = "Biznesi nuk u gjet." });

        return Ok(ToAdminBusinessDto(business));
    }

    [HttpPatch("businesses/{id}/approve")]
    public Task<ActionResult<AdminBusinessDto>> Approve(string id, CancellationToken cancellationToken) =>
        UpdateBusinessStatus(id, BusinessStatus.Approved, cancellationToken);

    [HttpPatch("businesses/{id}/reject")]
    public Task<ActionResult<AdminBusinessDto>> Reject(string id, CancellationToken cancellationToken) =>
        UpdateBusinessStatus(id, BusinessStatus.Rejected, cancellationToken);

    [HttpPatch("businesses/{id}/suspend")]
    public Task<ActionResult<AdminBusinessDto>> Suspend(string id, CancellationToken cancellationToken) =>
        UpdateBusinessStatus(id, BusinessStatus.Suspended, cancellationToken);

    private async Task<ActionResult<AdminBusinessDto>> UpdateBusinessStatus(string id, BusinessStatus status, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var businessId))
            return NotFound(new ErrorResponseDto { Message = "Biznesi nuk u gjet." });

        var entity = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
        if (entity is null)
            return NotFound(new ErrorResponseDto { Message = "Biznesi nuk u gjet." });

        entity.Status = status;
        entity.UpdatedAt = DateTime.UtcNow;

        var actorUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(actorUserIdValue, out var actorUserId))
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                Action = $"Business.{status}",
                EntityType = "Business",
                EntityId = entity.Id.ToString(),
                Metadata = $"{{\"newStatus\":\"{status}\"}}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(ToAdminBusinessDto(entity));
    }

    private static AdminBusinessDto ToAdminBusinessDto(Business b)
    {
        return new AdminBusinessDto
        {
            Id = b.Id.ToString(),
            Name = b.BusinessName,
            OwnerId = b.OwnerId.ToString(),
            City = b.City,
            BusinessType = b.BusinessType.ToString(),
            Status = b.Status.ToString(),
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        };
    }
}
