namespace BusinessDirectory.Application.Dtos.Promotions;

public sealed class GetPromotionsQueryDto
{
    public string? Category { get; set; }
    public Guid? BusinessId { get; set; }
    public bool OnlyActive { get; set; } = true;
}
