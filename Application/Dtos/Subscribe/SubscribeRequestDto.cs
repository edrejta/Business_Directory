using System.ComponentModel.DataAnnotations;

namespace BusinessDirectory.Application.Dtos.Subscribe;

public sealed class SubscribeRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
