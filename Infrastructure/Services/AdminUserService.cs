using BusinessDirectory.Application.Dtos.User;
using BusinessDirectory.Application.Interfaces;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class AdminUserService : IAdminUserService
{
    private readonly ApplicationDbContext _db;

    public AdminUserService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
