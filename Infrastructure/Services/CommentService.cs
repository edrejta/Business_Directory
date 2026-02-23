using BusinessDirectory.Application.Dtos.Comment;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BusinessDirectory.Infrastructure.Services;

public sealed class CommentService : ICommentService
{
    private readonly ApplicationDbContext _db;

    public CommentService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CommentDto> CreateAsync(Guid userId, CommentCreateDto dto, CancellationToken ct)
    {
        var businessExists = await _db.Businesses
            .AsNoTracking()
            .AnyAsync(b => b.Id == dto.BusinessId, ct);

        if (!businessExists)
            throw new InvalidOperationException("Business not found.");

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            BusinessId = dto.BusinessId,
            UserId = userId,
            Text = dto.Text.Trim(),
            Rate = dto.Rate,
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(ct);

        return new CommentDto
        {
            Id = comment.Id,
            BusinessId = comment.BusinessId,
            UserId = comment.UserId,
            Text = comment.Text,
            Rate = comment.Rate,
            CreatedAt = comment.CreatedAt
        };
    }

    public async Task<(CommentDto? Result, bool NotFound, bool Forbid, string? Error)> UpdateAsync(
        Guid id,
        Guid userId,
        CommentUpdateDto dto,
        CancellationToken ct)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id, ct);

        if (comment is null)
            return (null, true, false, null);

        if (comment.UserId != userId)
            return (null, false, true, null);

        comment.Text = dto.Text.Trim();
        comment.Rate = dto.Rate;

        await _db.SaveChangesAsync(ct);

        return (new CommentDto
        {
            Id = comment.Id,
            BusinessId = comment.BusinessId,
            UserId = comment.UserId,
            Text = comment.Text,
            Rate = comment.Rate,
            CreatedAt = comment.CreatedAt
        }, false, false, null);
    }

    public async Task<(bool NotFound, bool Forbid, string? Error)> DeleteAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id, ct);

        if (comment is null)
            return (true, false, null);

        if (comment.UserId != userId)
            return (false, true, null);

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync(ct);

        return (false, false, null);
    }
}
