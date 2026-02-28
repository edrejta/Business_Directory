using BusinessDirectory.Application.Dtos.City;

namespace BusinessDirectory.Application.Interfaces
{
    public interface ICityService
    {
        Task<IReadOnlyList<CityDto>> GetAllAsync(CancellationToken ct);
    }
}
