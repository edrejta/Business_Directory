namespace BusinessDirectory.Application.Dtos;

public class DashboardStatsDto
{
    public int UserCount { get; set; }
    public int BusinessCount { get; set; }
    public List<ActivityLog>? RecentActivityLogs { get; set; }
}
