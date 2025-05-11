using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Monzowler.Crawler.Contracts.HttpClient;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Parsers;
using Monzowler.Shared.Utilities;

namespace Monzowler.Application.Services.Parsers;

public class StaticHtmlParser(IApiClient http, ILogger<StaticHtmlParser> logger) : ISubParser
{
    private readonly ILogger<StaticHtmlParser> _logger = logger;

    public async Task<ParserResponse> ParseLinksAsync(ParserRequest request, CancellationToken ct)
    {
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

        return new ParserResponse
        {
            Links = links
        };
    }
}