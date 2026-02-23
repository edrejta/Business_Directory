using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Application.Dtos;

public sealed class BusinessDto
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public BusinessType BusinessType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public BusinessStatus Status { get; set; }
    public string? SuspensionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}
