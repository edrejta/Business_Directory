using System;

namespace BusinessDirectory.Domain.Entities;

public sealed class Favorite
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
