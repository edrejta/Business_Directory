namespace BusinessDirectory.Application.Dtos.OpenDays;

public sealed class OpenDaysResponseDto
{
    public Guid BusinessId { get; set; }
    public bool MondayOpen { get; set; }
    public bool TuesdayOpen { get; set; }
    public bool WednesdayOpen { get; set; }
    public bool ThursdayOpen { get; set; }
    public bool FridayOpen { get; set; }
    public bool SaturdayOpen { get; set; }
    public bool SundayOpen { get; set; }
}
