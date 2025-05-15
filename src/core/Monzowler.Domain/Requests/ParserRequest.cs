namespace Monzowler.Domain.Requests;

public class ParserRequest
{
    public required string Url { get; set; }
    public required string AllowedHost { get; set; }
}