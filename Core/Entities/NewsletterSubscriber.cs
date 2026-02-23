namespace BusinessDirectory.Domain.Entities;

public sealed class NewsletterSubscriber
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
