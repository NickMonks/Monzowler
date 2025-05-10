using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Monzowler.Crawler.Models;
using Monzowler.HttpClient.ApiClient;

namespace Monzowler.Crawler.Parsers;

public class StaticHtmlParser : ISubParser
{
    private readonly IApiClient _http;
    private readonly ILogger<StaticHtmlParser> _logger;

    public StaticHtmlParser(IConfiguration config, IApiClient http, ILogger<StaticHtmlParser> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ParserResponse> ParseLinksAsync(ParserRequest request, CancellationToken ct)
    {
        var response = await _http.GetStringAsync(request.Url, ct);
        var doc = new HtmlDocument();
        doc.LoadHtml(response);

        var nodes = doc.DocumentNode.SelectNodes("//a[@href]");
        if (nodes == null) return new ParserResponse
        {
            Links = new()
        };

        var links = nodes
            .Select(a => a.GetAttributeValue("href", string.Empty))
            .Select(href =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(href) || href.StartsWith("#")) return null;

                    var candidateUri = new Uri(new Uri(request.Url), href);

                    //only crawl http/https links - filter other types of links like mailto:, javascript:, etc
                    if (candidateUri.Scheme != Uri.UriSchemeHttp && candidateUri.Scheme != Uri.UriSchemeHttps)
                        return null;

                    var path = candidateUri.AbsolutePath.ToLowerInvariant();
                    var excludedExtensions = new[] {
                        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar", ".jpg", ".png", ".gif", ".mp4", ".mp3", ".m4a"
                    };

                    if (excludedExtensions.Any(ext => path.EndsWith(ext)))
                        return null;

                    return candidateUri.ToString().TrimEnd('/');
                }
                catch
                {
                    return null;
                }
            })
            .Where(u => u is not null && new Uri(u).Host == request.AllowedHost)
            .Distinct()
            .ToList();

        return new ParserResponse
        {
            Links = links
        };
    }
}