using BusinessDirectory.Application.Dtos.User;

namespace BusinessDirectory.Application.Interfaces;

public interface IAdminUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
