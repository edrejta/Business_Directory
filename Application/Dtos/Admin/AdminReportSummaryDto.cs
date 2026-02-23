namespace BusinessDirectory.Application.Dtos.Admin;

public sealed class AdminReportSummaryDto
{
    public int TotalUsers { get; set; }
    public int TotalBusinesses { get; set; }
    public int ApprovedBusinesses { get; set; }
    public int PendingBusinesses { get; set; }
    public int RejectedBusinesses { get; set; }
    public int SuspendedBusinesses { get; set; }
    public int TotalComments { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
}
