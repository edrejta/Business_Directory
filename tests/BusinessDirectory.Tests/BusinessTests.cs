using BusinessDirectory.Domain.Entities;

namespace BusinessDirectory.Tests;

public sealed class BusinessTests
{
    [Fact]
    public void BusinessNumber_SetNull_StoresEmptyStringInBackingField()
    {
        var sut = new Business();

        sut.BusinessNumber = null!;

        Assert.Equal(string.Empty, sut.BusinesssNumber);
        Assert.Equal(string.Empty, sut.BusinessNumber);
    }

    [Fact]
    public void BusinessNumber_SetValue_UpdatesBackingField()
    {
        var sut = new Business();

        sut.BusinessNumber = "12345";

        Assert.Equal("12345", sut.BusinesssNumber);
        Assert.Equal("12345", sut.BusinessNumber);
    }
}
