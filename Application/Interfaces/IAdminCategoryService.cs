using BusinessDirectory.Application.Dtos.Admin;

namespace BusinessDirectory.Application.Interfaces;

public interface IAdminCategoryService
{
    Task<IReadOnlyList<AdminCategoryStatsDto>> GetCategoryStatsAsync(CancellationToken cancellationToken = default);
}
