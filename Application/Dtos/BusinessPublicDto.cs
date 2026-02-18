namespace BusinessDirectory.Application.Dtos;

public class BusinessPublicDto
{
    
    public int Id { get; set; } 
    public string BusinessName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}