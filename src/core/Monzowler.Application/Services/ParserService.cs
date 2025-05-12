using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Parsers;
using Monzowler.Domain.Entities;
using Monzowler.Domain.Responses;
using Monzowler.Shared.Observability;

namespace Monzowler.Application.Services;

/// <summary>
/// This class will try to run several parsers if one of them fails. It is likely that some domains are JS-Heavy,
/// which might require us to use a headless browser and parse from it.
/// If that is the case we will fall back to this instead. 
/// </summary>
public class ParserService(IEnumerable<ISubParser> parsers, ILogger<ParserService> logger) : IParser
{
    private readonly List<ISubParser> _parsers = parsers.ToList();
    public async Task<ParserResponse> ParseLinksAsync(ParserRequest request, CancellationToken ct)
    {
        using var span = TracingHelper.Source.StartActivity("ParseLinks");
        span?.SetTag("url", request.Url);

        foreach (var parser in _parsers)
        {
            using var childSpan = TracingHelper.Source.StartActivity("ParserAttempt");
            childSpan?.SetTag("type", parser.GetType().Name);

            try
            {
                var response = await parser.ParseLinksAsync(request, ct);

                if (response.Links.Count > 0)
                {
                    childSpan?.SetTag("status", ParserStatusCode.Ok.ToString());
                    childSpan?.SetTag("linkCount", response.Links.Count);
                    childSpan?.AddEvent(new ActivityEvent("ParserSuccess"));

                    return new ParserResponse
                    {
                        Links = response.Links,
                        StatusCode = ParserStatusCode.Ok
                    };
                }

                //If no links are found there is the possibility that the links are within scripts tags
                //therefore we need to try our renderered parser.
                if (response.HasScriptTags)
                {
                    childSpan?.AddEvent(new ActivityEvent("HasScriptTags_ConsiderFallback"));
                    continue;
                }

                childSpan?.SetTag("status", ParserStatusCode.NoLinksFound.ToString());
                childSpan?.AddEvent(new ActivityEvent("NoLinksFound"));
                return new ParserResponse
                {
                    Links = new(),
                    StatusCode = ParserStatusCode.NoLinksFound
                };
            }
            catch (HttpRequestException ex)
            {
                var status = ex.StatusCode switch
                {
                    HttpStatusCode.RequestTimeout => ParserStatusCode.TimeoutError,
                    HttpStatusCode.NotFound => ParserStatusCode.NotFoundError,
                    HttpStatusCode.Forbidden => ParserStatusCode.Forbidden,
                    >= HttpStatusCode.InternalServerError and < HttpStatusCode.NetworkAuthenticationRequired
                        => ParserStatusCode.ServerError,
                    _ => ParserStatusCode.HttpError
                };

                childSpan?.SetStatus(ActivityStatusCode.Error, status.ToString());
                childSpan?.SetTag("httpStatusCode", ex.StatusCode?.ToString());
                childSpan?.SetTag("status", status.ToString());
                childSpan?.AddEvent(new ActivityEvent("HttpException"));

                logger.LogWarning("Http Exception occurred for {Url}: {Status}", request.Url, ex.StatusCode);

                return new ParserResponse
                {
                    Links = [],
                    StatusCode = status
                };
            }
            catch (Exception ex)
            {
                childSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
                childSpan?.AddEvent(new ActivityEvent("ParsingFailed"));

                logger.LogWarning(ex, "ParserService {ParserService} failed for {Url}",
                    parser.GetType().Name, request.Url);
            }
        }

        span?.SetStatus(ActivityStatusCode.Error, "AllParsersFailed");
        span?.AddEvent(new ActivityEvent("AllParsersFailed"));

        logger.LogWarning("All parsers failed for {Url}", request.Url);
        return new ParserResponse
        {
            Links = [],
            StatusCode = ParserStatusCode.ParserError
        };
    }

}