using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Monzowler.Crawler.Models;
using Monzowler.Domain.Entities;

namespace Monzowler.Application.Session;

/// <summary>
/// Represent a unique session of the crawler, meaning when the crawler starts it will
/// instantiate and encapsulate a concurrent list of pages, the channel where we will produce/consume
/// pages from, etc. 
/// </summary>
public class CrawlSession
{
    public ConcurrentBag<Page> Pages { get; } = new();
    public ConcurrentDictionary<string, bool> Visited { get; } = new();
    
    //TODO: consider make it bounded - we have heavy producers vs consumers so introducing
    //backpressure could be nice
    public Channel<Link> ChannelSession { get; } = Channel.CreateUnbounded<Link>();
    public Item Item { get; } = new();

    public async Task<bool> TryEnqueueAsync(Link link, ILogger logger)
    {
        try
        {
            await ChannelSession.Writer.WriteAsync(link);
            Item.Increment();
            logger.LogDebug("Enqueued: {Url}", link.Url);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enqueue {Url}", link.Url);
            return false;
        }
    }
}

/// <summary>
/// Represents the number of active items that are currently for processing in our session channel
/// This is critical to ensure we don't close too soon the writers channel from our current sessions
/// </summary>
public class Item
{
    private int _activeCount;

    public void Increment()
    {
        Interlocked.Increment(ref _activeCount);
    }

    public void Decrement()
    {
        Interlocked.Decrement(ref _activeCount);
    }

    public int Count => _activeCount;

    public bool IsEmpty => _activeCount == 0;
}
