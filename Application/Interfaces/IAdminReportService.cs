using BusinessDirectory.Application.Dtos;

namespace BusinessDirectory.Application.Interfaces;

public interface IAdminReportService
{
    Task<AdminReportSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
