using System.ComponentModel.DataAnnotations;

namespace BusinessDirectory.Application.Dtos.Comment;

public sealed class CommentCreateDto
{
    [Required]
    public Guid BusinessId { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(2000)]
    public string Text { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rate { get; set; }
}
