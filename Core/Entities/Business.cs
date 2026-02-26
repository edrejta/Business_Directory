using BusinessDirectory.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessDirectory.Domain.Entities;

public sealed class Business
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public string BusinessName { get; set; } = null!;

    public string BusinesssNumber { get; set; } = string.Empty;

    [NotMapped]
    public string BusinessNumber
    {
        get => BusinesssNumber;
        set => BusinesssNumber = value ?? string.Empty;
    }

    public string Address { get; set; } = null!;

    public string City { get; set; } = null!;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public BusinessType BusinessType { get; set; }

    public string Description { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public BusinessStatus Status { get; set; } = BusinessStatus.Pending;

    public string? SuspensionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int OpenDaysMask { get; set; } = 127;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

    public string WebsiteUrl { get; set; } = string.Empty;
}