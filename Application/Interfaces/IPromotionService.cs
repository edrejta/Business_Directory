using BusinessDirectory.Application.Dtos.Promotions;

namespace BusinessDirectory.Application.Interfaces;

public interface IPromotionService
{
    Task<IReadOnlyList<PromotionResponseDto>> GetAsync(GetPromotionsQueryDto query, CancellationToken ct);

    Task<(PromotionResponseDto? Result, bool NotFound, bool Forbid, string? Error)> CreateAsync(
        Guid actorUserId,
        CreatePromotionRequestDto request,
        CancellationToken ct);

    Task<IReadOnlyList<PromotionResponseDto>> GetMineAsync(
        Guid actorUserId,
        Guid? businessId,
        string? category,
        CancellationToken ct);

    Task<(PromotionResponseDto? Result, bool NotFound, bool Forbid, string? Error)> UpdateAsync(
        Guid actorUserId,
        Guid promotionId,
        UpdatePromotionRequestDto request,
        CancellationToken ct);

    Task<(bool NotFound, bool Forbid, string? Error)> DeleteAsync(
        Guid actorUserId,
        Guid promotionId,
        CancellationToken ct);
}