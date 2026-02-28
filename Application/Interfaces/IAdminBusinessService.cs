using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Dtos.Businesses;
using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Application.Interfaces;

public interface IAdminBusinessService
{
    Task<IReadOnlyList<BusinessDto>> GetAllAsync(BusinessStatus? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessDto>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<BusinessDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(BusinessDto? Result, bool NotFound, bool Conflict, string? Error)> ApproveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(bool NotFound, bool Forbid, bool Conflict, string? Error)> DeleteAsync(
        Guid id,
        Guid actorUserId,
        string? reason,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
    Task<(BusinessDto? Result, bool NotFound, bool Conflict, string? Error)> SuspendAsync(
        Guid id,
        Guid actorUserId,
        string? reason,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}
