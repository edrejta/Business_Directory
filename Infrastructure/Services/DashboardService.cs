using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        return new DashboardStatsDto
        {
            UserCount = await _context.Users.CountAsync(cancellationToken),
            BusinessCount = await _context.Businesses.CountAsync(cancellationToken),

            RecentActivityLogs = await _context.ActivityLogs
                .Include(a => a.User) 
                .OrderByDescending(a => a.CreatedAt)
                .Take(20)
                .ToListAsync(cancellationToken)
        };
    }
}
