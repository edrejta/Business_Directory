using BusinessDirectory.Application.Dtos.City;
using BusinessDirectory.Application.Interfaces;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BusinessDirectory.Infrastructure.Services
{
    public sealed class CityService : ICityService
    {
        private readonly ApplicationDbContext _db;

        public CityService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<CityDto>> GetAllAsync(CancellationToken ct)
        {
            return await _db.Cities.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new CityDto { Id = c.Id, Name = c.Name })
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<CityDto>> SearchAsync(string? search, int take, CancellationToken ct)
        {
            take = take <= 0 ? 20 : Math.Min(take, 50);

            var query = _db.Cities.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(c => c.Name.Contains(s));
            }

            return await query
                .OrderBy(c => c.Name)
                .Take(take)
                .Select(c => new CityDto { Id = c.Id, Name = c.Name })
                .ToListAsync(ct);
        }
    }
}
