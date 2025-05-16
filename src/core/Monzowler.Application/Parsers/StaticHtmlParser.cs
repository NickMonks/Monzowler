using System.Diagnostics;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Monzowler.Application.Contracts.HttpClient;
using Monzowler.Application.Contracts.Services;
using Monzowler.Domain.Requests;
using Monzowler.Domain.Responses;
using Monzowler.Shared.Observability;
using Monzowler.Shared.Utilities;

namespace Monzowler.Application.Parsers;

public class StaticHtmlParser(ILogger<StaticHtmlParser> logger) : ISubParser
{
    public async Task<ParserResponse> ParseLinksAsync(ParserRequest request, CancellationToken ct)
    {
        logger.LogInformation("Start parsing links - {ParserName}", nameof(StaticHtmlParser));
        using var span = TracingHelper.Source.StartActivity(nameof(StaticHtmlParser), ActivityKind.Internal);

        var doc = new HtmlDocument();
        doc.LoadHtml(request.HtmlResult);

        List<string?> links = [];
        var nodes = doc.DocumentNode.SelectNodes("//a[@href]");
        if (nodes != null)
        {
            links = nodes
                .Select(a => a.GetAttributeValue("href", string.Empty))
                .Select(href => Sanitizer.SanitizeUrl(href, request.Url))
                .Where(u => u is not null && new Uri(u).Host == request.AllowedHost)
                .Distinct()
                .ToList();
        }

        var hasScriptTags = doc.DocumentNode.SelectSingleNode("//script") is not null;

        return new ParserResponse
        {
            Links = links,
            HasScriptTags = hasScriptTags
        };
    }
}