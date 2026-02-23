using BusinessDirectory.Application.Dtos.Comment;

namespace BusinessDirectory.Application.Interfaces;

public interface ICommentService
{
    Task<CommentDto> CreateAsync(Guid userId, CommentCreateDto dto, CancellationToken ct);

    Task<(CommentDto? Result, bool NotFound, bool Forbid, string? Error)> UpdateAsync(
        Guid id,
        Guid userId,
        CommentUpdateDto dto,
        CancellationToken ct);

    Task<(bool NotFound, bool Forbid, string? Error)> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken ct);
}
