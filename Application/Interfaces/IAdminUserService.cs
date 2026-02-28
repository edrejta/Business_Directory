using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Dtos.User;
using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Application.Interfaces;

public interface IAdminUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminAuditLogDto>> GetAuditLogsAsync(int take, CancellationToken cancellationToken = default);
    Task<(bool NotFound, bool Forbid, string? Error, UserDto? Result)> UpdateRoleAsync(
        Guid actorUserId,
        Guid targetUserId,
        UserRole newRole,
        string? reason,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
    Task<(bool NotFound, bool Forbid, bool Conflict, string? Error)> DeleteUserAsync(
        Guid actorUserId,
        Guid targetUserId,
        string? reason,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}
