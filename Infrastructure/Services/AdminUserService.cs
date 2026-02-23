using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Dtos.User;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Entities;
using BusinessDirectory.Domain.Enums;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class AdminUserService : IAdminUserService
{
    private readonly ApplicationDbContext _db;

    public AdminUserService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminAuditLogDto>> GetAuditLogsAsync(int take, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 500);

        return await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .Select(a => new AdminAuditLogDto
            {
                Id = a.Id,
                ActorUserId = a.ActorUserId,
                Action = a.Action,
                TargetUserId = a.TargetUserId,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                Reason = a.Reason,
                CreatedAt = a.CreatedAt,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool NotFound, bool Forbid, string? Error, UserDto? Result)> UpdateRoleAsync(
        Guid actorUserId,
        Guid targetUserId,
        UserRole newRole,
        string? reason,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var actorIsAdmin = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == actorUserId && u.Role == UserRole.Admin, cancellationToken);
        if (!actorIsAdmin)
            return (false, true, "Only admins can change user roles.", null);

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);
        if (target is null)
            return (true, false, null, null);

        if (actorUserId == targetUserId && target.Role == UserRole.Admin && newRole != UserRole.Admin)
            return (false, true, "You cannot demote your own admin account.", null);

        if (target.Role == newRole)
            return (false, false, "User already has this role.", null);

        if (target.Role == UserRole.Admin && newRole != UserRole.Admin)
        {
            var adminCount = await _db.Users.CountAsync(u => u.Role == UserRole.Admin, cancellationToken);
            if (adminCount <= 1)
                return (false, false, "Cannot demote the last admin user.", null);
        }

        var oldRole = target.Role;
        var now = DateTime.UtcNow;
        var cleanReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var cleanIp = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
        var cleanUserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();

        // Keep role update and audit insert together.
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        target.Role = newRole;
        target.UpdatedAt = now;

        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = "USER_ROLE_UPDATED",
            TargetUserId = targetUserId,
            OldValue = oldRole.ToString(),
            NewValue = newRole.ToString(),
            Reason = cleanReason,
            CreatedAt = now,
            IpAddress = cleanIp,
            UserAgent = cleanUserAgent
        });

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return (false, false, null, new UserDto
        {
            Id = target.Id,
            Username = target.Username,
            Email = target.Email,
            Role = target.Role,
            CreatedAt = target.CreatedAt,
            UpdatedAt = target.UpdatedAt
        });
    }

    public async Task<(bool NotFound, bool Forbid, string? Error)> DeleteUserAsync(
        Guid actorUserId,
        Guid targetUserId,
        string? reason,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var actorIsAdmin = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == actorUserId && u.Role == UserRole.Admin, cancellationToken);
        if (!actorIsAdmin)
            return (false, true, "Only admins can delete users.");

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);
        if (target is null)
            return (true, false, null);

        if (actorUserId == targetUserId)
            return (false, true, "You cannot delete your own account.");

        if (target.Role == UserRole.Admin)
        {
            var adminCount = await _db.Users.CountAsync(u => u.Role == UserRole.Admin, cancellationToken);
            if (adminCount <= 1)
                return (false, false, "Cannot delete the last admin user.");
        }

        var hasBusinesses = await _db.Businesses.AnyAsync(b => b.OwnerId == targetUserId, cancellationToken);
        if (hasBusinesses)
            return (false, false, "User owns businesses. Delete/reassign those first.");

        var hasComments = await _db.Comments.AnyAsync(c => c.UserId == targetUserId, cancellationToken);
        if (hasComments)
            return (false, false, "User has comments. Delete those first.");

        var cleanReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var cleanIp = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
        var cleanUserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
        var now = DateTime.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = "USER_DELETED",
            TargetUserId = targetUserId,
            OldValue = target.Role.ToString(),
            NewValue = "DELETED",
            Reason = cleanReason,
            CreatedAt = now,
            IpAddress = cleanIp,
            UserAgent = cleanUserAgent
        });

        _db.Users.Remove(target);

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return (false, false, null);
    }
}
