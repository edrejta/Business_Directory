using BusinessDirectory.Application.Dtos.Businesses;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Entities;
using BusinessDirectory.Domain.Enums;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class AdminBusinessService : IAdminBusinessService
{
    private readonly ApplicationDbContext _db;

    public AdminBusinessService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BusinessDto>> GetAllAsync(BusinessStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Businesses.AsNoTracking();

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .Select(ToDtoExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BusinessDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Businesses
            .AsNoTracking()
            .Where(b => b.Status == BusinessStatus.Pending)
            .OrderByDescending(b => b.CreatedAt)
            .Select(ToDtoExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<BusinessDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Businesses
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(ToDtoExpression())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(BusinessDto? Result, bool NotFound, bool Conflict, string? Error)> ApproveAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (business is null)
            return (null, true, false, null);

        if (business.Status == BusinessStatus.Approved)
            return (null, false, true, "Business is already approved.");

        business.Status = BusinessStatus.Approved;

        // When a business gets approved, elevate its owner to BusinessOwner if they are still a regular user.
        var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == business.OwnerId, cancellationToken);
        if (owner is not null && owner.Role == UserRole.User)
        {
            owner.Role = UserRole.BusinessOwner;
            owner.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return (ToDto(business), false, false, null);
    }

    public async Task<(bool NotFound, bool Forbid, bool Conflict, string? Error)> DeleteAsync(
        Guid id,
        Guid actorUserId,
        string? reason,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (business is null)
            return (true, false, false, null);

        var actorIsAdmin = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == actorUserId && u.Role == UserRole.Admin, cancellationToken);
        if (!actorIsAdmin)
            return (false, true, false, "Only admins can delete businesses.");

        if (business.Status != BusinessStatus.Pending)
            return (false, false, true, "Only pending businesses can be deleted.");

        var cleanReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var cleanIp = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
        var cleanUserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
        var now = DateTime.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = "BUSINESS_DELETED",
            TargetUserId = business.OwnerId,
            OldValue = business.Status.ToString(),
            NewValue = "DELETED",
            Reason = cleanReason,
            CreatedAt = now,
            IpAddress = cleanIp,
            UserAgent = cleanUserAgent
        });

        _db.Businesses.Remove(business);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(cancellationToken);
            return (false, false, true, "Business cannot be deleted due to related data constraints.");
        }

        return (false, false, false, null);
    }

    public async Task<(BusinessDto? Result, bool NotFound, bool Conflict, string? Error)> SuspendAsync(
        Guid id,
        Guid actorUserId,
        string? reason,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (business is null)
            return (null, true, false, null);

        if (business.Status == BusinessStatus.Suspended)
            return (null, false, true, "Business is already suspended.");

        if (business.Status != BusinessStatus.Approved)
            return (null, false, false, "Only approved businesses can be suspended.");

        var actorIsAdmin = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == actorUserId && u.Role == UserRole.Admin, cancellationToken);
        if (!actorIsAdmin)
            return (null, false, false, "Only admins can suspend businesses.");

        var cleanReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var cleanIp = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
        var cleanUserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
        var now = DateTime.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        business.Status = BusinessStatus.Suspended;
        business.SuspensionReason = cleanReason;

        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = "BUSINESS_SUSPENDED",
            TargetUserId = business.OwnerId,
            OldValue = BusinessStatus.Approved.ToString(),
            NewValue = BusinessStatus.Suspended.ToString(),
            Reason = cleanReason,
            CreatedAt = now,
            IpAddress = cleanIp,
            UserAgent = cleanUserAgent
        });

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return (ToDto(business), false, false, null);
    }

    private static string OpenDaysFromMask(int mask)
    {
        var days = new List<string>(7);
        if ((mask & 1) != 0) days.Add("Mon");
        if ((mask & 2) != 0) days.Add("Tue");
        if ((mask & 4) != 0) days.Add("Wed");
        if ((mask & 8) != 0) days.Add("Thu");
        if ((mask & 16) != 0) days.Add("Fri");
        if ((mask & 32) != 0) days.Add("Sat");
        if ((mask & 64) != 0) days.Add("Sun");
        return days.Count == 0 ? string.Empty : string.Join(", ", days);
    }

    private static System.Linq.Expressions.Expression<Func<Business, BusinessDto>> ToDtoExpression()
    {
        return b => new BusinessDto
        {
            Id = b.Id,
            OwnerId = b.OwnerId,
            BusinessName = b.BusinessName,
            Type = b.BusinessType.ToString(),
            BusinessType = b.BusinessType,
            Address = b.Address,
            City = b.City,
            Email = b.Email,
            PhoneNumber = b.PhoneNumber,
            Description = b.Description,
            ImageUrl = b.ImageUrl,
            BusinessUrl = b.WebsiteUrl,
            Status = b.Status,
            SuspensionReason = b.SuspensionReason,
            CreatedAt = b.CreatedAt,
            BusinessNumber = b.BusinesssNumber,
            OpenDays = ""
        };
    }

    private static BusinessDto ToDto(Business business)
    {
        return new BusinessDto
        {
            Id = business.Id,
            OwnerId = business.OwnerId,
            BusinessName = business.BusinessName,
            Type = business.BusinessType.ToString(),
            BusinessType = business.BusinessType,
            Address = business.Address,
            City = business.City,
            Email = business.Email,
            PhoneNumber = business.PhoneNumber,
            Description = business.Description,
            ImageUrl = business.ImageUrl,
            BusinessUrl = business.WebsiteUrl,
            Status = business.Status,
            SuspensionReason = business.SuspensionReason,
            CreatedAt = business.CreatedAt,
            BusinessNumber = business.BusinesssNumber,
            OpenDays = OpenDaysFromMask(business.OpenDaysMask),
            IsFavorite = false
        };
    }
}
