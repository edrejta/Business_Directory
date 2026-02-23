namespace BusinessDirectory.Application.Dtos.Comment;

public sealed class CommentDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Rate { get; set; }
    public DateTime CreatedAt { get; set; }
}
