namespace BusinessDirectory.Application.Dtos;

public sealed class CommentCreateDto
{
    public Guid BusinessId { get; set; }
    public Guid UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Rate { get; set; }
}
