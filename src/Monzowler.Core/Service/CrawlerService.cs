using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monzowler.Crawler.Interfaces;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Parsers;
using Monzowler.Crawler.Settings;
using Monzowler.HttpClient.Throttler;

namespace Monzowler.Crawler.Service;

public class CrawlerService(
    IParser parser,
    RobotsTxtService robots,
    ISitemapRepository repo,
    ILogger<CrawlerService> logger,
    PolitenessThrottler throttler,
    IOptions<CrawlerOptions> options)
    : ICrawlerService
{
    private readonly CrawlerOptions _opts = options.Value;

    public async Task<Dictionary<string, List<string>>> CrawlAsync(string rootUrl)
{
    var sitemap = new ConcurrentDictionary<string, List<string>>();
    var visited = new ConcurrentDictionary<string, bool>();
    var channel = Channel.CreateUnbounded<Link>();
    var baseUri = new Uri(rootUrl);
    var rootHost = baseUri.Host;
    

    var (disallows, crawlDelay) = await robots.GetRulesAsync(rootUrl);
    throttler.SetDelay(rootHost, crawlDelay ?? 0);

    int writersRemaining = 0;

    async Task<bool> TryEnqueueAsync(Link link)
    {
        try
        {
            await channel.Writer.WriteAsync(link);
            Interlocked.Increment(ref writersRemaining);
            logger.LogDebug("Enqueued: {Url}, writersRemaining: {Writers}", link.Url, writersRemaining);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enqueue {Url}", link.Url);
            return false;
        }
    }

    await TryEnqueueAsync(new Link
    {
        Url = rootUrl,
        Domain = rootHost,
        Depth = 0,
        Retries = 0
    });

    var workers = Enumerable.Range(0, _opts.MaxConcurrency).Select(_ => Task.Run(async () =>
    {
        await foreach (var item in channel.Reader.ReadAllAsync())
        {
            if (item.Depth > _opts.MaxDepth) continue;

            var path = new Uri(item.Url).AbsolutePath;
            if (!robots.IsAllowed(path, disallows)) continue;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                logger.LogInformation("Crawling: {Url} (Depth: {Depth})", item.Url, item.Depth);

                var links = await parser.ParseLinksAsync(item.Url, rootHost, cts.Token);
                sitemap[item.Url] = links;

                foreach (var link in links.Where(l => l is not null))
                {
                    try
                    {
                        var linkHost = new Uri(link).Host;
                        var newDepth = item.Depth + 1;
                        var linkPath = new Uri(link).AbsolutePath;


                        if (linkHost == rootHost &&
                            newDepth <= _opts.MaxDepth &&
                            !visited.ContainsKey(link) &&
                            robots.IsAllowed(linkPath, disallows))
                        {
                            var newLink = new Link
                            {
                                Url = link,
                                Domain = rootHost,
                                Depth = newDepth,
                                Retries = 0
                            };

                            if (await TryEnqueueAsync(newLink))
                            {
                                visited.TryAdd(link, true);
                            }
                        }
                    }
                    catch
                    {
                        logger.LogWarning("Invalid link: {Link}", link);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Timeout on {Url}, retrying", item.Url);

                if (item.Retries < _opts.MaxRetries)
                {
                    visited.TryRemove(item.Url, out bool _);

                    await TryEnqueueAsync(new Link
                    {
                        Url = item.Url,
                        Domain = rootHost,
                        Depth = item.Depth,
                        Retries = item.Retries + 1
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing {Url}", item.Url);
            }
            finally
            {
                var remaining = Interlocked.Decrement(ref writersRemaining);
                logger.LogDebug("Finished: {Url}, writersRemaining: {Remaining}", item.Url, remaining);

                if (remaining == 0)
                {
                    channel.Writer.Complete();
                }
            }
        }
    }));

    await Task.WhenAll(workers);
    await repo.SaveSitemapAsync(rootUrl, sitemap);
    return sitemap.ToDictionary(kv => kv.Key, kv => kv.Value);
}

}
