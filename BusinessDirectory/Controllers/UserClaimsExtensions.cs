using System.Security.Claims;

namespace BusinessDirectory.Controllers;

public static class UserClaimsExtensions
{
    public static Guid? GetActorUserId(this ClaimsPrincipal user)
    {
        var userIdValue =
            user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue("id")
            ?? user.FindFirstValue("userId");

        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
