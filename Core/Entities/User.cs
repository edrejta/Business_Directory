using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Domain.Entities;

public sealed class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; } //save as hash
    public string Email { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; } =  DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } =  DateTime.UtcNow;

    public ICollection<Business> OwnedBusinesses { get; set; } = new List<Business>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

}