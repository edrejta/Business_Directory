using BusinessDirectory.Application.Dtos.Auth;
using BusinessDirectory.Application.Dtos.User;

namespace BusinessDirectory.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(UserCreateDto dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDto?> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
}
