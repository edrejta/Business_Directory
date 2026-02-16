namespace BusinessDirectory.Application.Dtos;

/// <summary>
/// Inputet për Signup – vijnë nga formularët e frontend-it, jo nga databaza.
/// </summary>
public sealed class UserCreateDto
{
    /// <summary>Emri i përdoruesit – input nga klienti.</summary>
    [System.ComponentModel.DataAnnotations.Required]
    public string Username { get; set; } = string.Empty;
    /// <summary>Email – input nga klienti.</summary>
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = string.Empty;
    /// <summary>Fjalëkalimi – input nga klienti.</summary>
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MinLength(8)]
    public string Password { get; set; } = string.Empty; 
}
