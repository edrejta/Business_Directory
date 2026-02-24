namespace BusinessDirectory.Application.Dtos.Reviews;

public sealed class GetReviewsQueryDto
{
    public Guid? BusinessId { get; set; }
    public int Limit { get; set; } = 20;
}
