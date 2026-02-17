using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Interfaces;
using BusinessDirectory.Domain.Entities;
using BusinessDirectory.Domain.Enums;
using Infrastructure;

namespace BusinessDirectory.Infrastructure.Services
{
    public class BusinessService : IBusinessService
    {
        private readonly ApplicationDbContext _db;

        public BusinessService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Guid> CreateAsync(BusinessCreateDto dto, Guid userId)
        {
            var business = new Business
            {
                OwnerId = userId,
                BusinessName = dto.BusinessName,
                Address = dto.Address,
                City = dto.City,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                BusinessType = dto.BusinessType,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,

                Status = BusinessStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.Businesses.Add(business);
            await _db.SaveChangesAsync();

            return business.Id;
        }
    }
}
