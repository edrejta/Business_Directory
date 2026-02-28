using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // save as hash
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Business> OwnedBusinesses { get; set; } = new List<Business>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
