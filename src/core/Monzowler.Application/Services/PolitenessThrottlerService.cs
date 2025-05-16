using System.Collections.Concurrent;
using System.Diagnostics;
using Monzowler.Application.Contracts.Services;
using Monzowler.Shared.Observability;

namespace Monzowler.Application.Services;

/// <summary>
/// Some websites have a Crawl-delay requirement, which we should respect. This service
/// throttler the request for a given domain if the last request was done before the demanded
/// interval. Despite not been part of the official robot txt protocol is a nice addition to our crawler!
/// </summary>
public class PolitenessThrottlerService : IPolitenessThrottlerService
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
        var now = DateTime.UtcNow;

        if (!_crawlDelays.TryGetValue(domain, out var delayMs))
        {
            return;
        }

        if (_lastRequestTimes.TryGetValue(domain, out var lastTime))
        {
            var elapsed = now - lastTime;
            var remaining = TimeSpan.FromMilliseconds(delayMs) - elapsed;

            if (remaining > TimeSpan.Zero)
            {
                using var span = TracingHelper.Source.StartActivity("EnforcePoliteness");
                span?.SetTag("domain", domain);
                span?.AddEvent(new ActivityEvent("ThrottlingStarted"));
                await Task.Delay(remaining, ct);
                span?.AddEvent(new ActivityEvent("ThrottlingEnded"));
            }
        }

        _lastRequestTimes[domain] = now;
    }
}
