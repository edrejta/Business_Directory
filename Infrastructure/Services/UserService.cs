using BusinessDirectory.Application.Dtos.User;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Enums;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BusinessDirectory.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly ApplicationDbContext _db;

    public UserService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<UserDto?> GetMeAsync(Guid userId, CancellationToken ct)
        => GetByIdAsync(userId, ct);

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<(bool NotFound, bool Forbid, string? Error, UserDto? Result)> UpdateAsync(
        Guid id,
        Guid currentUserId,
        UserUpdateDto dto,
        CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return (true, false, null, null);

        
        if (id != currentUserId)
            return (false, true, null, null);

        
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Email))
            return (false, false, "Username and Email are required.", null);

      
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var emailTaken = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == normalizedEmail && u.Id != id, ct);

        if (emailTaken)
            return (false, false, "Email already exists.", null);

        user.Username = dto.Username.Trim();
        user.Email = normalizedEmail;

        

        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return (false, false, null, new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }

    public async Task<(bool NotFound, UserDto? Result)> UpdateRoleAsync(Guid userId, UserRole role, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return (true, null);

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return (false, new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }
}
