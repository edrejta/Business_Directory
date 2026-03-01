using BusinessDirectory.Application.Dtos.Subscribe;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BusinessDirectory.Infrastructure.Services;

public sealed class SubscribeService : ISubscribeService
{
    private readonly ApplicationDbContext _db;

    public SubscribeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<SubscribeResponseDto> SubscribeAsync(SubscribeRequestDto request, CancellationToken ct)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.", nameof(request));

        var email = request.Email.Trim().ToLowerInvariant();
        var exists = await _db.NewsletterSubscribers.AnyAsync(x => x.Email == email, ct);
        if (!exists)
        {
            _db.NewsletterSubscribers.Add(new NewsletterSubscriber
            {
                Id = Guid.NewGuid(),
                Email = email,
                CreatedAt = DateTime.UtcNow
            });
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Concurrent subscribe request may win the unique index race.
                var nowExists = await _db.NewsletterSubscribers
                    .AsNoTracking()
                    .AnyAsync(x => x.Email == email, ct);
                if (!nowExists)
                    throw;

                return new SubscribeResponseDto
                {
                    Message = "Email is already subscribed.",
                    Created = false,
                    AlreadySubscribed = true
                };
            }

            return new SubscribeResponseDto
            {
                Message = "Subscribed successfully.",
                Created = true,
                AlreadySubscribed = false
            };
        }

        return new SubscribeResponseDto
        {
            Message = "Email is already subscribed.",
            Created = false,
            AlreadySubscribed = true
        };
    }
}
