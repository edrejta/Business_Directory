using BusinessDirectory.Application.Dtos;

namespace BusinessDirectory.Application.Interfaces;

public interface IAdminUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
