using Microsoft.Extensions.Logging;

namespace Monzowler.Crawler.Parsers;

/// <summary>
/// This class will try to run several parsers if one of them fails. It is likely that some domains are JS-Heavy,
/// which might require us to use a headless browser and parse from it. If that is the case we will fallback to this instead. 
/// </summary>
public class Parser(IEnumerable<ISubParser> parsers, ILogger<Parser> logger) : IParser
{
    //TODO: maybe optimise this - if we know a domain is JS-heavy why try htlm parser and fail?
    private readonly List<ISubParser> _parsers = parsers.ToList();

    public async Task<List<string?>> ParseLinksAsync(string url, string allowedHost, CancellationToken ct) {
        foreach (var parser in _parsers) {
            try {
                var links = await parser.ParseLinksAsync(url, allowedHost, ct);
                if (links.Count > 0) return links;
            } catch (Exception ex) {
                logger.LogWarning(ex, "Parser {Parser} failed for {Url}", parser.GetType().Name, url);
            }
        }

        logger.LogWarning("All parsers failed for {Url}", url);
        return new List<string?>();
    }
}