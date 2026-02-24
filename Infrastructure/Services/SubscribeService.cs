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
            await _db.SaveChangesAsync(ct);
        }

        return new SubscribeResponseDto { Message = "Success" };
    }
}
