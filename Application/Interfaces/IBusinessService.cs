using BusinessDirectory.Application.Dtos;

namespace BusinessDirectory.Application.Interfaces
{
    public interface IBusinessService
    {
        Task<Guid> CreateAsync(BusinessCreateDto dto, Guid userId);
    }
}
