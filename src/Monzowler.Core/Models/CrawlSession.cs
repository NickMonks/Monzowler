using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Monzowler.Crawler.Models;

namespace Monzowler.Crawler;

public class CrawlSession
{
    public ConcurrentBag<Page> Pages { get; } = new();
    public ConcurrentDictionary<string, bool> Visited { get; } = new();
    public Channel<Link> ChannelSession { get; set; } = Channel.CreateUnbounded<Link>();
    private int _writersRemaining = 0;

    public int WritersRemaining => _writersRemaining;

    public async Task<bool> TryEnqueueAsync(Link link, ILogger logger)
    {
        try
        {
            await ChannelSession.Writer.WriteAsync(link);
            Interlocked.Increment(ref _writersRemaining);
            logger.LogDebug("Enqueued: {Url}", link.Url);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enqueue {Url}", link.Url);
            return false;
        }
    }

    public int DecrementWriters()
    {
        return Interlocked.Decrement(ref _writersRemaining);
    }
}