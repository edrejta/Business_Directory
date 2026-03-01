namespace BusinessDirectory.Application.Dtos.Businesses;

public sealed class BusinessPublicDto
{
    public Guid Id { get; set; }
    public string? BusinessName { get; set; }
    public string? Description { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? Category { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? OpenDays { get; set; }
    public string? ImageUrl { get; set; }
}