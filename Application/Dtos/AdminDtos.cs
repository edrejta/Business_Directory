namespace BusinessDirectory.Application.Dtos;

public sealed class DashboardMetricsDto
{
    public int TotalBusinesses { get; set; }
    public int PendingBusinesses { get; set; }
    public int ApprovedBusinesses { get; set; }
    public int TotalUsers { get; set; }
}

public sealed class DashboardActivityDto
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

public sealed class AdminDashboardDto
{
    public DashboardMetricsDto Metrics { get; set; } = new();
    public List<DashboardActivityDto> RecentActivity { get; set; } = new();
}

public sealed class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ReportsByReasonDto
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class ReportSummaryDto
{
    public int TotalReports { get; set; }
    public int OpenReports { get; set; }
    public int ResolvedReports { get; set; }
    public int FlaggedBusinesses { get; set; }
    public List<ReportsByReasonDto> ReportsByReason { get; set; } = new();
}

public sealed class AdminCategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int BusinessesCount { get; set; }
}

public sealed class AdminBusinessDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
