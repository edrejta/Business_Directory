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
        // DbContext is not thread-safe; run queries sequentially.
        var totalUsers = await _db.Users.AsNoTracking().CountAsync(cancellationToken);
        var totalBusinesses = await _db.Businesses.AsNoTracking().CountAsync(cancellationToken);
        var totalComments = await _db.Comments.AsNoTracking().CountAsync(cancellationToken);

        var statusCounts = await _db.Businesses
            .AsNoTracking()
            .GroupBy(b => b.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        return new AdminReportSummaryDto
        {
            TotalUsers = totalUsers,
            TotalBusinesses = totalBusinesses,
            TotalComments = totalComments,
            ApprovedBusinesses = statusCounts.GetValueOrDefault(BusinessStatus.Approved),
            PendingBusinesses = statusCounts.GetValueOrDefault(BusinessStatus.Pending),
            RejectedBusinesses = statusCounts.GetValueOrDefault(BusinessStatus.Rejected),
            SuspendedBusinesses = statusCounts.GetValueOrDefault(BusinessStatus.Suspended),
            GeneratedAtUtc = DateTime.UtcNow
        };
    }
}
