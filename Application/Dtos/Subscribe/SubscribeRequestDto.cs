using System.ComponentModel.DataAnnotations;

namespace BusinessDirectory.Application.Dtos.Subscribe;

public sealed class SubscribeRequestDto
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}
