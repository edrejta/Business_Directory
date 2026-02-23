using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Application.Dtos.Businesses;

public sealed class BusinessCreateDto
{
    public string BusinessName { get; set; } = string.Empty;
    
    public string Address { get; set; } = string.Empty;
    
    public string City { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string PhoneNumber { get; set; } = string.Empty;
    
    public BusinessType BusinessType { get; set; }
    
    public string Description { get; set; } = string.Empty;
    
    public string ImageUrl { get; set; } = string.Empty;
    
    public string BusinessNumber { get; set; } = string.Empty;
}
