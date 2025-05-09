namespace Monzowler.Crawler.Models;

/// <summary>
/// Parser status code based on the response 
/// </summary>
public enum ParserStatusCode
{
    Ok,
    NoLinksFound,
    ServerError,
    TimeoutError,
    NotFoundError,
    HttpError,
    ParserError,
    UnknownError
}