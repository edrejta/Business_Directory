using BusinessDirectory.Application.Dtos;
using BusinessDirectory.Application.Dtos.User;


namespace BusinessDirectory.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetMeAsync(Guid userId, CancellationToken ct);
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct);
    

    Task<(bool NotFound, bool Forbid, string? Error, UserDto? Result)> UpdateAsync(
        Guid id,
        Guid currentUserId,
        UserUpdateDto dto,
        CancellationToken ct);
}
