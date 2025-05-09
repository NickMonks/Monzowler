using System.Collections.Concurrent;

namespace Monzowler.HttpClient.Throttler;

/// <summary>
/// Some websites have a Crawl-delay requirement, which we should respect. This service
/// throttler the request for a given domain if the last request was done before the demanded
/// interval. Despite not been part of the official robot txt protocol is a nice addition to our crawler!
/// </summary>
public class PolitenessThrottler
{
    private readonly ConcurrentDictionary<string, DateTime> _lastRequestTimes = new();
    private readonly ConcurrentDictionary<string, int> _crawlDelays = new(); // in milliseconds!

    /// <summary>
    /// Set the politeness delay for a given domain (in milliseconds).
    /// </summary>
    public void SetDelay(string domain, int delayMs)
    {
        _crawlDelays[domain] = delayMs;
    }
    
    public async Task EnforceAsync(string domain, CancellationToken ct = default)
    {
        //If we don't find it we haven't set it up - therefore no delay
        var now = DateTime.UtcNow;

        if (!_crawlDelays.TryGetValue(domain, out var delayMs)) return;

        if (_lastRequestTimes.TryGetValue(domain, out var lastTime))
        {
            
            var elapsed = now - lastTime;
            var remaining = TimeSpan.FromMilliseconds(delayMs) - elapsed;

            if (remaining > TimeSpan.Zero)
            {
                //As we are using async delay it shouldn't block our worker thread
                await Task.Delay(remaining, ct);
            }
        }

        _lastRequestTimes[domain] = now;
    }
}
