namespace Monzowler.Domain.Requests;

public class ParserRequest
{
    public required string Url { get; init; }
    public required string HtmlResult { get; init; }
    public required string AllowedHost { get; init; }
}