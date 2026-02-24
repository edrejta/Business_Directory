using BusinessDirectory.Application.Dtos.Subscribe;

namespace BusinessDirectory.Application.Interfaces;

public interface ISubscribeService
{
    Task<SubscribeResponseDto> SubscribeAsync(SubscribeRequestDto request, CancellationToken ct);
}
