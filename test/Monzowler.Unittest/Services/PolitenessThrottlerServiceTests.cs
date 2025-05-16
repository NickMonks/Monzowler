namespace Monzowler.Unittest.Services;

using Monzowler.Application.Services;
using Xunit;

public class PolitenessThrottlerServiceTests
{
    private readonly PolitenessThrottlerService _throttler;
    private const string Domain = "example.com";

    public PolitenessThrottlerServiceTests()
    {
        _throttler = new PolitenessThrottlerService();
    }

    [Fact]
    public async Task EnforceAsync_DoesNotDelay_IfNoDelaySet()
    {
        //Arrange & Act
        var start = DateTime.UtcNow;
        await _throttler.EnforceAsync(Domain);
        var elapsed = DateTime.UtcNow - start;

        //Assert
        Assert.True(elapsed < TimeSpan.FromMilliseconds(100), "Should not delay if no delay set");
    }

    [Fact]
    public async Task EnforceAsync_DelaysCorrectly_OnRepeatedCallWithinDelay()
    {
        //Arrange
        var delayMs = 300;
        _throttler.SetDelay(Domain, delayMs);

        //Act
        await _throttler.EnforceAsync(Domain);

        var start = DateTime.UtcNow;
        await _throttler.EnforceAsync(Domain);
        var elapsed = DateTime.UtcNow - start;

        //Assert - between start and elapsed it shoul've happen more than 300ms
        Assert.True(elapsed >= TimeSpan.FromMilliseconds(delayMs), $"Should delay at least {delayMs}ms");
    }

    [Fact]
    public async Task EnforceAsync_DoesNotDelay_AfterEnoughTimeHasPassed()
    {

        var delayMs = 200;
        _throttler.SetDelay(Domain, delayMs);

        // first call sets the timestamp and we simulate that more than 100ms happened
        await _throttler.EnforceAsync(Domain);
        await Task.Delay(delayMs + 100);

        //Act
        var start = DateTime.UtcNow;
        await _throttler.EnforceAsync(Domain);
        var elapsed = DateTime.UtcNow - start;

        // Arrange - Should NOT delay
        Assert.True(elapsed < TimeSpan.FromMilliseconds(100), "Should not delay if sufficient time passed");
    }
}
