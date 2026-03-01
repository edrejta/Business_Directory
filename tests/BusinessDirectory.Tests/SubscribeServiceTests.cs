using BusinessDirectory.Infrastructure.Services;

namespace BusinessDirectory.Tests;

public sealed class SubscribeServiceTests
{
    [Fact]
    public async Task SubscribeAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        var sut = new SubscribeService(db: null!);

        var act = async () => await sut.SubscribeAsync(request: null!, CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }
}
