namespace BusinessDirectory.Application.Dtos.Subscribe;

public sealed class SubscribeResponseDto
{
    public string Message { get; set; } = "Success";
    public bool Created { get; set; }
    public bool AlreadySubscribed { get; set; }
}
