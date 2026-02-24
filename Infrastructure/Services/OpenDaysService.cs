using BusinessDirectory.Application.Dtos.OpenDays;
using BusinessDirectory.Application.Interfaces;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BusinessDirectory.Infrastructure.Services;

public sealed class OpenDaysService : IOpenDaysService
{
    private readonly ApplicationDbContext _db;

    public OpenDaysService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<OpenDaysResponseDto?> GetPublicAsync(Guid businessId, CancellationToken ct)
    {
        var business = await _db.Businesses
            .AsNoTracking()
            .Where(b => b.Id == businessId)
            .Select(b => new { b.Id, b.OpenDaysMask })
            .FirstOrDefaultAsync(ct);

        return business is null ? null : MapOpenDaysDto(business.Id, business.OpenDaysMask);
    }

    public async Task<(OpenDaysResponseDto? Result, bool NotFound, bool Forbid)> GetOwnerAsync(
        Guid businessId,
        Guid ownerId,
        CancellationToken ct)
    {
        var business = await _db.Businesses
            .AsNoTracking()
            .Where(b => b.Id == businessId)
            .Select(b => new { b.Id, b.OwnerId, b.OpenDaysMask })
            .FirstOrDefaultAsync(ct);

        if (business is null)
            return (null, true, false);

        if (business.OwnerId != ownerId)
            return (null, false, true);

        return (MapOpenDaysDto(business.Id, business.OpenDaysMask), false, false);
    }

    public async Task<(OpenDaysResponseDto? Result, bool NotFound, bool Forbid)> SetOwnerAsync(
        Guid ownerId,
        OwnerUpdateOpenDaysRequestDto request,
        CancellationToken ct)
    {
        var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == request.BusinessId, ct);
        if (business is null)
            return (null, true, false);

        if (business.OwnerId != ownerId)
            return (null, false, true);

        business.OpenDaysMask = BuildOpenDaysMask(request);
        await _db.SaveChangesAsync(ct);

        return (MapOpenDaysDto(business.Id, business.OpenDaysMask), false, false);
    }

    private static OpenDaysResponseDto MapOpenDaysDto(Guid businessId, int mask)
    {
        return new OpenDaysResponseDto
        {
            BusinessId = businessId,
            MondayOpen = IsDayOpen(mask, 0),
            TuesdayOpen = IsDayOpen(mask, 1),
            WednesdayOpen = IsDayOpen(mask, 2),
            ThursdayOpen = IsDayOpen(mask, 3),
            FridayOpen = IsDayOpen(mask, 4),
            SaturdayOpen = IsDayOpen(mask, 5),
            SundayOpen = IsDayOpen(mask, 6)
        };
    }

    private static int BuildOpenDaysMask(OwnerUpdateOpenDaysRequestDto request)
    {
        var mask = 0;
        if (request.MondayOpen) mask |= (1 << 0);
        if (request.TuesdayOpen) mask |= (1 << 1);
        if (request.WednesdayOpen) mask |= (1 << 2);
        if (request.ThursdayOpen) mask |= (1 << 3);
        if (request.FridayOpen) mask |= (1 << 4);
        if (request.SaturdayOpen) mask |= (1 << 5);
        if (request.SundayOpen) mask |= (1 << 6);
        return mask;
    }

    private static bool IsDayOpen(int mask, int bit) => (mask & (1 << bit)) != 0;
}
