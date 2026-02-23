namespace BusinessDirectory.Application.Dtos.Businesses;

public class BusinessPublicDto
{
    public Guid Id { get; set; }
    
    public string BusinessName { get; set; } = null!;
    
    public string Description { get; set; } = null!;
    
    public string City { get; set; } = null!;
    
    public string? Category { get; set; }
}
