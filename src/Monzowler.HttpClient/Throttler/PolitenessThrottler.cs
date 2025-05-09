using System.Collections.Concurrent;

namespace Monzowler.HttpClient.Throttler;

public class PolitenessThrottler
{
    private readonly ConcurrentDictionary<string, DateTime> _lastRequestTimes = new();
    private readonly ConcurrentDictionary<string, int> _crawlDelays = new(); // in milliseconds

    /// <summary>
    /// Set the politeness delay for a given domain (in milliseconds).
    /// </summary>
    public void SetDelay(string domain, int delayMs)
    {
        _crawlDelays[domain] = delayMs;
    }
    
    public async Task EnforceAsync(string domain, CancellationToken ct = default)
    {
        if (!_crawlDelays.TryGetValue(domain, out var delayMs)) return;

        var now = DateTime.UtcNow;

        if (_lastRequestTimes.TryGetValue(domain, out var lastTime))
        {
            var elapsed = now - lastTime;
            var remaining = TimeSpan.FromMilliseconds(delayMs) - elapsed;

            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, ct);
            }
        }

        _lastRequestTimes[domain] = DateTime.UtcNow;
    }
}
