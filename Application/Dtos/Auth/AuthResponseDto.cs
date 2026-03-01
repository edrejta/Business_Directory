using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Application.Dtos.Auth;

public sealed class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}
