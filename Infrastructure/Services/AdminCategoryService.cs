using BusinessDirectory.Application.Dtos.Admin;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Enums;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class AdminCategoryService : IAdminCategoryService
{
    private readonly ApplicationDbContext _db;

    public AdminCategoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AdminCategoryStatsDto>> GetCategoryStatsAsync(CancellationToken cancellationToken = default)
    {
        var grouped = await _db.Businesses
            .AsNoTracking()
            .GroupBy(b => b.BusinessType)
            .Select(g => new AdminCategoryStatsDto
            {
                Category = g.Key,
                TotalBusinesses = g.Count(),
                ApprovedBusinesses = g.Count(b => b.Status == BusinessStatus.Approved),
                PendingBusinesses = g.Count(b => b.Status == BusinessStatus.Pending),
                RejectedBusinesses = g.Count(b => b.Status == BusinessStatus.Rejected),
                SuspendedBusinesses = g.Count(b => b.Status == BusinessStatus.Suspended)
            })
            .ToDictionaryAsync(x => x.Category, cancellationToken);

        return Enum.GetValues<BusinessType>()
            .OrderBy(x => (int)x)
            .Select(type => grouped.GetValueOrDefault(type) ?? new AdminCategoryStatsDto { Category = type })
            .ToList();
    }
}
