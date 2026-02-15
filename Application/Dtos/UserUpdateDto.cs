using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Application.Dtos;

public sealed class UserUpdateDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
