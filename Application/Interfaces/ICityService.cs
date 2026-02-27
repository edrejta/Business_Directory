using BusinessDirectory.Application.Dtos.City;

namespace BusinessDirectory.Application.Interfaces
{
    public interface ICityService
    {
        Task<List<CityDto>> GetAllAsync(CancellationToken ct);

        Task<List<CityDto>> SearchAsync(string? search, int take, CancellationToken ct);
    }
}
