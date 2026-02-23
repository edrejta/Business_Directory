namespace BusinessDirectory.Domain.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
