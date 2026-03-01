using BusinessDirectory.Application.Dtos.Reviews;
using BusinessDirectory.Application.Interfaces;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BusinessDirectory.Infrastructure.Services;

public sealed class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _db;

    public ReviewService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ReviewResponseDto>> GetAsync(GetReviewsQueryDto query, CancellationToken ct)
    {
        var limit = Math.Clamp(query.Limit, 1, 100);
        var comments = _db.Comments
            .AsNoTracking()
            .AsQueryable();

        if (query.BusinessId.HasValue)
            comments = comments.Where(c => c.BusinessId == query.BusinessId.Value);

        return await comments
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .Select(c => new ReviewResponseDto
            {
                Id = c.Id,
                BusinessId = c.BusinessId,
                ReviewerName = c.User.Username,
                Rating = c.Rate,
                Comment = c.Text,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(ct);
    }
}
