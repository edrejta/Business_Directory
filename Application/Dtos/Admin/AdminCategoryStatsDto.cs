using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Application.Dtos.Admin;

public sealed class AdminCategoryStatsDto
{
    public BusinessType Category { get; set; }
    public int TotalBusinesses { get; set; }
    public int ApprovedBusinesses { get; set; }
    public int PendingBusinesses { get; set; }
    public int RejectedBusinesses { get; set; }
    public int SuspendedBusinesses { get; set; }
}
