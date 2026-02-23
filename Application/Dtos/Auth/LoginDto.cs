namespace BusinessDirectory.Application.Dtos.Auth;

/// <summary>
/// Inputet për Login – vijnë nga formularët e frontend-it, jo nga databaza.
/// </summary>
public sealed class LoginDto
{
    /// <summary>Email – input nga klienti.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Fjalëkalimi – input nga klienti.</summary>
    public string Password { get; set; } = string.Empty;
}
