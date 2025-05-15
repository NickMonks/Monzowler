using Monzowler.Crawler.Models;
using Monzowler.Domain.Requests;
using Monzowler.Domain.Responses;

namespace Monzowler.Crawler.Parsers;

public interface IParser
{
    Task<ParserResponse> ParseLinksAsync(ParserRequest request, CancellationToken ct);
}

/// <summary>
/// This dummy interface is used so we can inject both Headless parser and Html parser and use it in
/// a composite pattern
/// </summary>
public interface ISubParser : IParser { }