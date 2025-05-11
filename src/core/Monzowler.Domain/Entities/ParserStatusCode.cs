namespace Monzowler.Domain.Entities;

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
    Forbidden,
    ParserError,
    Disallowed,
    UnknownError
}