using System.ComponentModel.DataAnnotations;

namespace BusinessDirectory.Application.Dtos.OpenDays;

public sealed class GetOpenDaysQueryDto
{
    [Required]
    public Guid BusinessId { get; set; }
}
