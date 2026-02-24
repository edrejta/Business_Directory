using BusinessDirectory.Application.Dtos.Promotions;

namespace BusinessDirectory.Application.Interfaces;

public interface IPromotionService
{
    Task<IReadOnlyList<PromotionResponseDto>> GetAsync(GetPromotionsQueryDto query, CancellationToken ct);

    Task<(PromotionResponseDto? Result, bool NotFound, bool Forbid, string? Error)> CreateAsync(
        Guid actorUserId,
        CreatePromotionRequestDto request,
        CancellationToken ct);
}
