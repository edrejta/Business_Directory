namespace BusinessDirectory.Domain.Entities;

public sealed class ActivityLog
{
    public Guid Id { get; set; }

    // FK -> User
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Action performed (CREATE, UPDATE, DELETE, LOGIN, etc.)
    public string Action { get; set; } = null!;

    // Entity affected (Business, Comment, User, etc.)
    public string Entity { get; set; } = null!;

    // Optional details
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
