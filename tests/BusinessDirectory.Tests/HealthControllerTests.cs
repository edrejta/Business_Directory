using BusinessDirectory.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BusinessDirectory.Tests;

public sealed class HealthControllerTests
{
    [Fact]
    public void Health_ReturnsOkWithStatusAndVersion()
    {
        var sut = new HealthController();

        var result = sut.Health();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);

        var payloadType = ok.Value!.GetType();
        var status = payloadType.GetProperty("status")?.GetValue(ok.Value) as string;
        var version = payloadType.GetProperty("version")?.GetValue(ok.Value) as string;

        Assert.Equal("ok", status);
        Assert.False(string.IsNullOrWhiteSpace(version));
    }
}
