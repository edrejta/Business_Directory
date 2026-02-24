using BusinessDirectory.Application.Dtos.Reviews;

namespace BusinessDirectory.Application.Interfaces;

public interface IReviewService
{
    Task<IReadOnlyList<ReviewResponseDto>> GetAsync(GetReviewsQueryDto query, CancellationToken ct);
}
