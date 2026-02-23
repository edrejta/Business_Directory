namespace BusinessDirectory.Application.Dtos;

public sealed class CoordinatesDto
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}

public sealed class PublicBusinessDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool Featured { get; set; }
    public List<string> Badges { get; set; } = new();
    public CoordinatesDto? Coordinates { get; set; }
    public double? Distance { get; set; }
}

public sealed class PublicBusinessDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int ReviewsCount { get; set; }
    public CoordinatesDto? Coordinates { get; set; }
}

public sealed class DealDto
{
    public Guid Id { get; set; }
    public Guid? BusinessId { get; set; }
    public string? BusinessName { get; set; }
    public string? BusinessImage { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Discounts";
    public decimal? OriginalPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public int? DiscountPercent { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public sealed class CreateDealRequestDto
{
    public Guid? BusinessId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Discounts"; // Discounts | FlashSales | EarlyAccess
    public decimal? OriginalPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public sealed class ReviewDto
{
    public Guid Id { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public sealed class SubscribeRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public sealed class MessageResponseDto
{
    public string Message { get; set; } = string.Empty;
}

public sealed class ErrorResponseDto
{
    public string Message { get; set; } = string.Empty;
}

public sealed class BusinessOpenDaysDto
{
    public Guid BusinessId { get; set; }
    public bool MondayOpen { get; set; }
    public bool TuesdayOpen { get; set; }
    public bool WednesdayOpen { get; set; }
    public bool ThursdayOpen { get; set; }
    public bool FridayOpen { get; set; }
    public bool SaturdayOpen { get; set; }
    public bool SundayOpen { get; set; }
}

public sealed class UpdateOpenDaysRequestDto
{
    public Guid? BusinessId { get; set; }
    public bool MondayOpen { get; set; }
    public bool TuesdayOpen { get; set; }
    public bool WednesdayOpen { get; set; }
    public bool ThursdayOpen { get; set; }
    public bool FridayOpen { get; set; }
    public bool SaturdayOpen { get; set; }
    public bool SundayOpen { get; set; }
}

public sealed class VerifyEmailRequestDto
{
    public string Token { get; set; } = string.Empty;
}

public sealed class ResendVerificationRequestDto
{
    public string Email { get; set; } = string.Empty;
}
