namespace BusinessDirectory.Application.Dtos;

public sealed class CommentUpdateDto
{
    public string Text { get; set; } = string.Empty;
    public int Rate { get; set; }
}
