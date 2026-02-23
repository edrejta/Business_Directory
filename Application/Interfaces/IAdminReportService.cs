using BusinessDirectory.Application.Dtos.Admin;

namespace BusinessDirectory.Application.Interfaces;

public interface IAdminReportService
{
    Task<AdminReportSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
