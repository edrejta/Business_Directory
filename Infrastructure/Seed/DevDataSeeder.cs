using BusinessDirectory.Domain.Entities;
using BusinessDirectory.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Seed;

public static class DevDataSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        bool isDevelopment,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!isDevelopment)
            return;

        var seedOnStartup = configuration.GetValue("SeedOnStartup", true);
        if (!seedOnStartup)
            return;

        var hasAnyUsers = db.Users.Any();
        if (hasAnyUsers)
            return;

        var now = DateTime.UtcNow;
        var adminPassword = configuration["SeedAdminPassword"] ?? "Admin12345!";

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@business.local",
            Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            EmailVerified = true,
            Role = UserRole.Admin,
            CreatedAt = now,
            UpdatedAt = now
        };

        var owner = new User
        {
            Id = Guid.NewGuid(),
            Username = "owner.demo",
            Email = "owner@business.local",
            Password = BCrypt.Net.BCrypt.HashPassword("Owner12345!"),
            EmailVerified = true,
            Role = UserRole.BusinessOwner,
            CreatedAt = now,
            UpdatedAt = now
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "user.demo",
            Email = "user@business.local",
            Password = BCrypt.Net.BCrypt.HashPassword("User12345!"),
            EmailVerified = true,
            Role = UserRole.User,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Users.AddRange(admin, owner, user);

        var businesses = new List<Business>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OwnerId = owner.Id,
                BusinessName = "Artisan Bistro",
                Address = "Main St 12",
                City = "Prishtine",
                Email = "hello@artisan.local",
                PhoneNumber = "+38344111222",
                BusinessType = BusinessType.Restaurant,
                Description = "Bistro me menu sezonale.",
                ImageUrl = "https://example.com/bistro.png",
                Status = BusinessStatus.Approved,
                Featured = true,
                Latitude = 42.6629m,
                Longitude = 21.1655m,
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                OwnerId = owner.Id,
                BusinessName = "Downtown Cafe",
                Address = "Rr. Nena Tereze 5",
                City = "Prishtine",
                Email = "info@downtown.local",
                PhoneNumber = "+38344111333",
                BusinessType = BusinessType.Cafe,
                Description = "Cafe me ambience moderne.",
                ImageUrl = "https://example.com/cafe.png",
                Status = BusinessStatus.Approved,
                Featured = true,
                Latitude = 42.6590m,
                Longitude = 21.1605m,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                OwnerId = owner.Id,
                BusinessName = "QuickFix Service",
                Address = "Industrial Zone 3",
                City = "Prizren",
                Email = "team@quickfix.local",
                PhoneNumber = "+38344111444",
                BusinessType = BusinessType.Service,
                Description = "Servis i shpejte teknik.",
                ImageUrl = "https://example.com/service.png",
                Status = BusinessStatus.Pending,
                Featured = false,
                Latitude = 42.2139m,
                Longitude = 20.7397m,
                CreatedAt = now.AddDays(-8),
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                OwnerId = owner.Id,
                BusinessName = "City Shop",
                Address = "Center Mall",
                City = "Peje",
                Email = "sales@cityshop.local",
                PhoneNumber = "+38344111555",
                BusinessType = BusinessType.Shop,
                Description = "Dyqan urban me artikuj te ndryshem.",
                ImageUrl = "https://example.com/shop.png",
                Status = BusinessStatus.Rejected,
                Featured = false,
                Latitude = 42.6598m,
                Longitude = 20.2883m,
                CreatedAt = now.AddDays(-6),
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                OwnerId = owner.Id,
                BusinessName = "Neighborhood Eats",
                Address = "Old Town 8",
                City = "Gjakove",
                Email = "contact@neighboreats.local",
                PhoneNumber = "+38344111666",
                BusinessType = BusinessType.Restaurant,
                Description = "Restorant familjar.",
                ImageUrl = "https://example.com/eats.png",
                Status = BusinessStatus.Approved,
                Featured = false,
                Latitude = null,
                Longitude = null,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now
            }
        };

        db.Businesses.AddRange(businesses);

        var promotions = new List<Promotion>
        {
            new()
            {
                Id = Guid.NewGuid(),
                BusinessId = businesses[0].Id,
                Title = "Lunch Combo -20%",
                Description = "Oferta vlen cdo dite pune.",
                ExpiresAt = now.Date.AddDays(15),
                IsActive = true,
                CreatedAt = now.AddDays(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                BusinessId = businesses[1].Id,
                Title = "2 per 1 Espresso",
                Description = "Vetem pas ores 16:00.",
                ExpiresAt = now.Date.AddDays(7),
                IsActive = true,
                CreatedAt = now.AddDays(-1)
            }
        };
        db.Promotions.AddRange(promotions);

        var reviews = new List<Comment>
        {
            new()
            {
                Id = Guid.NewGuid(),
                BusinessId = businesses[0].Id,
                UserId = user.Id,
                Text = "Sherbim shume i mire dhe ushqim cilesor.",
                Rate = 5,
                CreatedAt = now.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                BusinessId = businesses[1].Id,
                UserId = user.Id,
                Text = "Atmosfere e qete, kafe shume e mire.",
                Rate = 4,
                CreatedAt = now.AddHours(-20)
            }
        };
        db.Comments.AddRange(reviews);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seed data u krijua: users={UsersCount}, businesses={BusinessesCount}, promotions={PromotionsCount}, reviews={ReviewsCount}.",
            3, businesses.Count, promotions.Count, reviews.Count);
    }
}
