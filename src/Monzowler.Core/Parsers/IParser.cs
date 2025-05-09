namespace Monzowler.Crawler.Parsers;

public interface IParser {
    Task<List<string?>> ParseLinksAsync(string url, string allowedHost, CancellationToken ct);
}

public interface ISubParser : IParser {}