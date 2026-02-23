using BusinessDirectory.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BusinessDirectory.Application.Dtos;

public sealed class UserCreateDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Range(0, 1)]
    public UserRole Role { get; set; } = UserRole.User;
}
