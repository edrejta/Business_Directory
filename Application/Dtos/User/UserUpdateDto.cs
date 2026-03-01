using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Application.Dtos.User;

public sealed class UserUpdateDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
