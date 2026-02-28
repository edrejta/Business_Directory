namespace BusinessDirectory.Domain.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = null!;
    public Guid? TargetUserId { get; set; }
    public string OldValue { get; set; } = null!;
    public string NewValue { get; set; } = null!;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public User ActorUser { get; set; } = null!;
    public User? TargetUser { get; set; }
}
