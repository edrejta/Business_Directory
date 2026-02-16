using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Domain.Entities;

public sealed class Business
{
    public int Id { get; set; }

    // FK -> User
    public Guid UserId { get; set; }
    public User Owner { get; set; } = null!;

    public string BusinessName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string City { get; set; } = null!;

    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;

    public BusinessType BusinessType { get; set; }

    public string Description { get; set; } = null!;

    // Store URL/path, not raw image bytes in the DB (recommended)
    public string ImageUrl { get; set; } = null!;

    public BusinessStatus Status { get; set; } = BusinessStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}