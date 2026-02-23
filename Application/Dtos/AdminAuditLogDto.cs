namespace BusinessDirectory.Application.Dtos;

public sealed class AdminAuditLogDto
{
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = null!;
    public Guid TargetUserId { get; set; }
    public string OldValue { get; set; } = null!;
    public string NewValue { get; set; } = null!;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
