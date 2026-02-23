using BusinessDirectory.Application.Dtos;
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
        await _db.SaveChangesAsync(cancellationToken);

        return (ToDto(business), false, false, null);
    }

    public async Task<(BusinessDto? Result, bool NotFound, bool Conflict, string? Error)> RejectAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (business is null)
            return (null, true, false, null);

        if (business.Status == BusinessStatus.Rejected)
            return (null, false, true, "Business is already rejected.");

        business.Status = BusinessStatus.Rejected;
        await _db.SaveChangesAsync(cancellationToken);

        return (ToDto(business), false, false, null);
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

    private static System.Linq.Expressions.Expression<Func<BusinessDirectory.Domain.Entities.Business, BusinessDto>> ToDtoExpression()
    {
        return b => new BusinessDto
        {
            Id = b.Id,
            OwnerId = b.OwnerId,
            BusinessName = b.BusinessName,
            Address = b.Address,
            City = b.City,
            Email = b.Email,
            PhoneNumber = b.PhoneNumber,
            BusinessType = b.BusinessType,
            Description = b.Description,
            ImageUrl = b.ImageUrl,
            Status = b.Status,
            SuspensionReason = b.SuspensionReason,
            CreatedAt = b.CreatedAt,
            BusinessNumber = b.BusinesssNumber
        };
    }

    private static BusinessDto ToDto(BusinessDirectory.Domain.Entities.Business business)
    {
        return new BusinessDto
        {
            Id = business.Id,
            OwnerId = business.OwnerId,
            BusinessName = business.BusinessName,
            Address = business.Address,
            City = business.City,
            Email = business.Email,
            PhoneNumber = business.PhoneNumber,
            BusinessType = business.BusinessType,
            Description = business.Description,
            ImageUrl = business.ImageUrl,
            Status = business.Status,
            SuspensionReason = business.SuspensionReason,
            CreatedAt = business.CreatedAt,
            BusinessNumber = business.BusinesssNumber
        };
    }
}
