using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monzowler.Crawler.Interfaces;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Parsers;
using Monzowler.Crawler.Settings;
using Monzowler.Persistence.Interfaces;
using Monzowler.Shared.Observability;

namespace Monzowler.Application.Services;

public class SpiderService(
    IParser parser,
    RobotsTxtService robots,
    ISiteMapRepository siteMapRepository,
    ILogger<SpiderService> logger,
    PolitenessThrottlerService throttler,
    IOptions<CrawlerOptions> options)
    : ISpiderService
{
    private readonly CrawlerOptions _opts = options.Value;

    public async Task<List<Page>> CrawlAsync(string rootUrl, string jobId)
    {
        using var span = TracingHelper.Source.StartActivity("CrawlJob", ActivityKind.Internal);
        span?.SetTag("rootUrl", rootUrl);
        span?.SetTag("jobId", jobId);

        var session = new CrawlSession();
        var baseUri = new Uri(rootUrl);
        var rootHost = baseUri.Host;

        var robotsTxtResponse = await robots.GetRulesAsync(rootUrl);
        throttler.SetDelay(rootHost, robotsTxtResponse.Delay);

        await session.TryEnqueueAsync(new Link
        {
            Url = rootUrl,
            Domain = rootHost,
            Depth = 0,
            Retries = 0
        }, logger);

        var workers = Enumerable.Range(0, _opts.MaxConcurrency)
            .Select(_ => Task.Run(() => ExecuteAsync(session, baseUri, robotsTxtResponse, jobId)));

        await Task.WhenAll(workers);

        span?.AddEvent(new ActivityEvent("CrawlCompleted"));
        span?.SetTag("pagesCrawled", session.Pages.Count);

        await siteMapRepository.SaveCrawlAsync(session.Pages.ToList());
        return session.Pages.ToList();
    }

    private async Task ExecuteAsync(CrawlSession session, Uri baseUri, RobotsTxtResponse robotsTxtResponse, string jobId)
{
    var rootHost = baseUri.Host;

    await foreach (var item in session.ChannelSession.Reader.ReadAllAsync())
    {
        using var span = TracingHelper.Source.StartActivity("CrawlPage", ActivityKind.Internal);
        span?.SetTag("url", item.Url);
        span?.SetTag("depth", item.Depth);
        span?.SetTag("jobId", jobId);
        span?.SetTag("retries", item.Retries);

        if (item.Depth > _opts.MaxDepth)
        {
            span?.AddEvent(new ActivityEvent("Skipped:MaxDepth"));
            continue;
        }

        var path = new Uri(item.Url).AbsolutePath;
        if (!robots.IsAllowed(path, robotsTxtResponse.Disallows, robotsTxtResponse.Allows))
        {
            span?.AddEvent(new ActivityEvent("Skipped:DisallowedByRobots"));
            continue;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_opts.Timeout));

        try
        {
            logger.LogInformation("Crawling: {Url} (Depth: {Depth})", item.Url, item.Depth);

            var parserResponse = await parser.ParseLinksAsync(new ParserRequest
            {
                Url = item.Url,
                AllowedHost = rootHost,
            }, cts.Token);

            span?.SetTag("statusCode", parserResponse.StatusCode.ToString());
            span?.SetTag("linkCount", parserResponse.Links.Count);
            span?.AddEvent(new ActivityEvent("ParsedSuccessfully"));

            session.Pages.Add(new Page
            {
                PageUrl = item.Url,
                Depth = item.Depth,
                Domain = rootHost,
                Links = parserResponse.Links,
                JobId = jobId,
                Status = parserResponse.StatusCode.ToString(),
                LastModified = DateTime.UtcNow.ToString("O"),
            });

            foreach (var link in parserResponse.Links.Where(l => l is not null))
            {
                try
                {
                    var linkUri = new Uri(link);
                    var newDepth = item.Depth + 1;
                    var linkPath = linkUri.AbsolutePath;

                    if (linkUri.Host == rootHost &&
                        newDepth <= _opts.MaxDepth &&
                        !session.Visited.ContainsKey(link) &&
                        robots.IsAllowed(linkPath, robotsTxtResponse.Disallows, robotsTxtResponse.Allows))
                    {
                        var newLink = new Link
                        {
                            Url = link,
                            Domain = rootHost,
                            Depth = newDepth,
                            Retries = 0
                        };

                        if (await session.TryEnqueueAsync(newLink, logger))
                        {
                            session.Visited.TryAdd(link, true);
                            span?.AddEvent(new ActivityEvent("LinkEnqueued"));
                        }
                    }
                }
                catch
                {
                    logger.LogWarning("Invalid link: {Link}", link);
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            span?.SetStatus(ActivityStatusCode.Error, "Timeout");
            span?.AddEvent(new ActivityEvent("Timeout"));

            logger.LogWarning("Operation was cancelled on {Url} due to timeout", item.Url);

            if (item.Retries < _opts.MaxRetries)
            {
                session.Visited.TryRemove(item.Url, out _);

                await session.TryEnqueueAsync(new Link
                {
                    Url = item.Url,
                    Domain = rootHost,
                    Depth = item.Depth,
                    Retries = item.Retries + 1
                }, logger);
            }
        }
        catch (Exception ex)
        {
            span?.SetStatus(ActivityStatusCode.Error, ex.Message);
            span?.AddEvent(new ActivityEvent("UnhandledError"));

            logger.LogError(ex, "Error processing {Url}", item.Url);
        }
        finally
        {
            var remaining = session.DecrementWriters();
            logger.LogDebug("Finished: {Url}, writersRemaining: {Remaining}", item.Url, remaining);

            if (remaining == 0)
            {
                session.ChannelSession.Writer.Complete();
            }
        }
    }
}

}
