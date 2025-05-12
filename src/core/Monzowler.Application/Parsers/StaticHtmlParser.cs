using System.Diagnostics;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Monzowler.Crawler.Contracts.HttpClient;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Parsers;
using Monzowler.Domain.Responses;
using Monzowler.Shared.Observability;
using Monzowler.Shared.Utilities;

namespace Monzowler.Application.Parsers;

public class StaticHtmlParser(IApiClient http, ILogger<StaticHtmlParser> logger) : ISubParser
{
    public async Task<ParserResponse> ParseLinksAsync(ParserRequest request, CancellationToken ct)
    {
        logger.LogInformation("Start parsing links - {ParserName}", nameof(StaticHtmlParser));
        using var span = TracingHelper.Source.StartActivity(nameof(StaticHtmlParser), ActivityKind.Internal);

        var response = await http.GetStringAsync(request.Url, ct);
        var doc = new HtmlDocument();
        doc.LoadHtml(response);

        var nodes = doc.DocumentNode.SelectNodes("//a[@href]");
        if (nodes == null) return new ParserResponse
        {
            Links = new()
        };

        var links = nodes
            .Select(a => a.GetAttributeValue("href", string.Empty))
            .Select(href => Sanitizer.SanitizeUrl(href, request.Url))
            .Where(u => u is not null && new Uri(u).Host == request.AllowedHost)
            .Distinct()
            .ToList();
        
        var hasScriptTags = doc.DocumentNode.SelectSingleNode("//script") is not null;

        return new ParserResponse
        {
            Links = links,
            HasScriptTags = hasScriptTags
        };
    }
}