using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Dtos.Businesses;
using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Application.Interfaces;

public interface IBusinessService
{
    Task<IReadOnlyList<BusinessDto>> GetApprovedAsync(
        string? search,
        string? city,
        BusinessType? type,
        CancellationToken ct);

    Task<BusinessDto?> GetApprovedByIdAsync(
        Guid id,
        CancellationToken ct);

    Task<BusinessDto> CreateAsync(
        BusinessCreateDto dto,
        Guid ownerId,
        CancellationToken ct);

    Task<(BusinessDto? Result, bool NotFound, bool Forbid, string? Error)> UpdateAsync(
        Guid id,
        BusinessUpdateDto dto,
        Guid ownerId,
        CancellationToken ct);

    Task<IReadOnlyList<BusinessDto>> GetMineAsync(
        Guid ownerId,
        BusinessStatus? status,
        CancellationToken ct);

    Task<(bool NotFound, bool Forbid, string? Error)> DeleteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken ct);

    Task<BusinessDto?> GetMineByIdAsync(
        Guid businessId,
        Guid ownerId,
        CancellationToken ct);
}
