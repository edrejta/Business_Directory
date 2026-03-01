using BusinessDirectory.Application.Dtos.OpenDays;

namespace BusinessDirectory.Application.Interfaces;

public interface IOpenDaysService
{
    Task<OpenDaysResponseDto?> GetPublicAsync(Guid businessId, CancellationToken ct);

    Task<(OpenDaysResponseDto? Result, bool NotFound, bool Forbid)> GetOwnerAsync(
        Guid businessId,
        Guid ownerId,
        CancellationToken ct);

    Task<(OpenDaysResponseDto? Result, bool NotFound, bool Forbid)> SetOwnerAsync(
        Guid ownerId,
        OwnerUpdateOpenDaysRequestDto request,
        CancellationToken ct);
}
