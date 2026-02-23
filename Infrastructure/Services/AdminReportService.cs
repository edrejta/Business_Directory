using BusinessDirectory.Application.Dtos.Admin;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Enums;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class AdminReportService : IAdminReportService
{
    private readonly ApplicationDbContext _db;

    public AdminReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AdminReportSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalUsersTask = _db.Users.AsNoTracking().CountAsync(cancellationToken);
        var totalBusinessesTask = _db.Businesses.AsNoTracking().CountAsync(cancellationToken);
        var totalCommentsTask = _db.Comments.AsNoTracking().CountAsync(cancellationToken);

        var statusCounts = await _db.Businesses
            .AsNoTracking()
            .GroupBy(b => b.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        await Task.WhenAll(totalUsersTask, totalBusinessesTask, totalCommentsTask);

        return new AdminReportSummaryDto
        {
            TotalUsers = totalUsersTask.Result,
            TotalBusinesses = totalBusinessesTask.Result,
            TotalComments = totalCommentsTask.Result,
            ApprovedBusinesses = statusCounts.GetValueOrDefault(BusinessStatus.Approved),
            PendingBusinesses = statusCounts.GetValueOrDefault(BusinessStatus.Pending),
            RejectedBusinesses = statusCounts.GetValueOrDefault(BusinessStatus.Rejected),
            SuspendedBusinesses = statusCounts.GetValueOrDefault(BusinessStatus.Suspended),
            GeneratedAtUtc = DateTime.UtcNow
        };
    }
}
