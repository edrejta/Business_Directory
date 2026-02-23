using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Application.Dtos.User;

public sealed class UserRoleUpdateDto
{
    public UserRole Role { get; set; }
    public string? Reason { get; set; }
}
