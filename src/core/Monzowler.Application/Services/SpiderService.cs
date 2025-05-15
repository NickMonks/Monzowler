using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monzowler.Application.Contracts.Results;
using Monzowler.Application.Contracts.Services;
using Monzowler.Application.Results;
using Monzowler.Application.Session;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Parsers;
using Monzowler.Domain.Entities;
using Monzowler.Domain.Requests;
using Monzowler.Domain.Responses;
using Monzowler.Shared.Observability;
using Monzowler.Shared.Settings;

namespace Monzowler.Application.Services;

public class SpiderService(
    IParser parser,
    IRobotsTxtService robots,
    IResultHandler resultHandler,
    ILogger<SpiderService> logger,
    IPolitenessThrottlerService throttler,
    IOptions<CrawlerSettings> options)
    : ISpiderService
{
    private readonly CrawlerSettings _opts = options.Value;

    public async Task<List<Page>> CrawlAsync(string rootUrl, string jobId)
    {
        using var span = TracingHelper.Source.StartActivity("CrawlJob", ActivityKind.Internal);
        span?.SetTag("rootUrl", rootUrl);
        span?.SetTag("jobId", jobId);

        var session = new CrawlSession();
        var baseUri = new Uri(rootUrl);
        var rootHost = baseUri.Host;

        var robotsTxtResponse = await robots.GetRulesAsync(rootUrl, _opts.UserAgent);
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

        await resultHandler.HandleAsync(session.Pages.ToList());
        return session.Pages.ToList();
    }

    private async Task ExecuteAsync(CrawlSession session, Uri baseUri, RobotsTxtResponse robotsTxtResponse, string jobId)
    {
        var rootHost = baseUri.Host;

        await foreach (var item in session.ChannelSession.Reader.ReadAllAsync())
        {
            using var span = TracingHelper.Source.StartActivity("CrawlPage");

            try
            {
                span?.SetTag("url", item.Url);
                span?.SetTag("depth", item.Depth);
                span?.SetTag("jobId", jobId);
                span?.SetTag("retries", item.Retries);

                if (item.Depth > _opts.MaxDepth)
                {
                    span?.AddEvent(new ActivityEvent("Skipped:MaxDepth"));
                    return;
                }

                var path = new Uri(item.Url).AbsolutePath;
                if (!robots.IsAllowed(path, robotsTxtResponse.Disallows, robotsTxtResponse.Allows))
                {
                    //We are not allowed to scrape the url. We still add the page on our job
                    span?.AddEvent(new ActivityEvent("Skipped:DisallowedByRobots"));
                    session.Pages.Add(new Page
                    {
                        PageUrl = item.Url,
                        Depth = item.Depth,
                        Domain = rootHost,
                        Links = [],
                        JobId = jobId,
                        Status = nameof(ParserStatusCode.Disallowed),
                        LastModified = DateTime.UtcNow.ToString("O"),
                    });
                    return;
                }

                //In order to avoid stuck operations/http calls after a set time we need to define a cancellation
                //token with a timeout, so when a certain threshold is surpassed we timeout the operation
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_opts.Timeout));

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

                foreach (var link in parserResponse.Links)
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
                        else if (!robots.IsAllowed(linkPath, robotsTxtResponse.Disallows, robotsTxtResponse.Allows))
                        {
                            // We want to record disallowed pages too 
                            session.Pages.Add(new Page
                            {
                                PageUrl = link,
                                Depth = newDepth,
                                Domain = rootHost,
                                Links = [],
                                JobId = jobId,
                                Status = nameof(ParserStatusCode.Disallowed),
                                LastModified = DateTime.UtcNow.ToString("O"),
                            });

                            logger.LogInformation("Disallowed link skipped and recorded: {Link}", link);
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
                logger.LogWarning("Operation was cancelled on {Url} due to timeout", item.Url);
                span?.SetStatus(ActivityStatusCode.Error, "Timeout");
                span?.AddEvent(new ActivityEvent("Timeout"));

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
                //There was an exception - we want to keep continue nonetheless 
                logger.LogError(ex, "Error processing {Url}", item.Url);
                span?.SetStatus(ActivityStatusCode.Error, ex.Message);
                span?.AddEvent(new ActivityEvent("UnhandledError"));
            }
            finally
            {
                session.Item.Decrement();
                logger.LogDebug("Finished: {Url}, items pending in channel: {Remaining}", item.Url, session.Item.Count);

                if (session.Item.IsEmpty)
                {
                    session.ChannelSession.Writer.Complete();
                }
            }
        }
    }

}
