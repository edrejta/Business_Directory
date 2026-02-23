using BusinessDirectory.Application.Dtos.Dashboard;

namespace BusinessDirectory.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}
