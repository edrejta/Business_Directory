namespace BusinessDirectory.Domain.Entities;

public sealed class Promotion
{
    public Guid Id { get; set; }
    public Guid? BusinessId { get; set; }
    public Business? Business { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
