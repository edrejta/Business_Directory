using BusinessDirectory.Domain.Entities;
using BusinessDirectory.Domain.Enums;

namespace BusinessDirectory.Tests;

public sealed class UserTests
{
    [Fact]
    public void NewUser_HasExpectedDefaultValues()
    {
        var user = new User();

        Assert.Equal(string.Empty, user.Username);
        Assert.Equal(string.Empty, user.Password);
        Assert.Equal(string.Empty, user.Email);
        Assert.Equal(UserRole.User, user.Role);
        Assert.NotEqual(default, user.CreatedAt);
        Assert.NotEqual(default, user.UpdatedAt);
    }
}
