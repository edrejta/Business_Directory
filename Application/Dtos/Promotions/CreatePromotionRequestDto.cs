using System.ComponentModel.DataAnnotations;

namespace BusinessDirectory.Application.Dtos.Promotions;

public sealed class CreatePromotionRequestDto
{
    [Required]
    public Guid BusinessId { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string Category { get; set; } = string.Empty;

    [Range(0, 99999999)]
    public decimal? OriginalPrice { get; set; }

    [Range(0, 99999999)]
    public decimal? DiscountedPrice { get; set; }

    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; } 
}
