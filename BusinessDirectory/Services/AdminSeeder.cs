using BusinessDirectory.Domain.Entities;
using BusinessDirectory.Domain.Enums;
using BusinessDirectory.Options;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BusinessDirectory.Services;

public sealed class AdminSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly AdminSeedOptions _options;

    public AdminSeeder(ApplicationDbContext db, IOptions<AdminSeedOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // ADMIN_SEED: make seeding opt-in via configuration.
        if (!_options.Enabled)
            return;

        var email = _options.Email.Trim().ToLowerInvariant();
        var username = _options.Username.Trim();
        var password = _options.Password.Trim();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("ADMIN_SEED: AdminSeed config is incomplete (Email/Username/Password).");
        }

        // ADMIN_SEED: idempotent check to avoid duplicates.
        var adminExists = await _db.Users.AnyAsync(
            u => u.Role == UserRole.Admin || u.Email == email,
            cancellationToken);

        if (adminExists)
            return;

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            Password = BCrypt.Net.BCrypt.HashPassword(password), // ADMIN_SEED: always hashed.
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(admin);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
