namespace BusinessDirectory.Domain.Entities;

public sealed class Comment
{
    public int Id { get; set; }

    // FK -> Business
    public int BusinessId { get; set; }
    public Business Business { get; set; } = null!;

    // FK -> User
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Text { get; set; } = null!;

    // Assuming 1..5
    public int Rate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}