using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Application.Dtos;

/// <summary>
/// Inputet për Signup – vijnë nga formularët e frontend-it, jo nga databaza.
/// </summary>
public sealed class UserCreateDto
{
    /// <summary>Emri i përdoruesit – input nga klienti.</summary>
    public string Username { get; set; } = string.Empty;
    /// <summary>Email – input nga klienti.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Fjalëkalimi – input nga klienti.</summary>
    public string Password { get; set; } = string.Empty;
    /// <summary>Roli (0=User, 1=BusinessOwner, 2=Admin) – input nga klienti.</summary>
    public UserRole Role { get; set; } = UserRole.User;
}
