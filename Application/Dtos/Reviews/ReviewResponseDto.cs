namespace BusinessDirectory.Application.Dtos.Reviews;

public sealed class ReviewResponseDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
