using BusinessDirectory.Application.Dtos;

namespace BusinessDirectory.Application.Interfaces
{
    public interface IBusinessService
    {
        Task<int> CreateAsync(BusinessCreateDto dto, Guid userId);
    }
}
