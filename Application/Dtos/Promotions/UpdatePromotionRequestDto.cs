namespace BusinessDirectory.Application.Dtos.Promotions;

public sealed class UpdatePromotionRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Discounts";

    public decimal? OriginalPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }

    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;
}