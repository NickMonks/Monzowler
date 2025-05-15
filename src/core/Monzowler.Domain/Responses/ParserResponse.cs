using Monzowler.Domain.Entities;

namespace Monzowler.Domain.Responses;

public class ParserResponse
{
    public required List<string> Links { get; init; }
    public ParserStatusCode StatusCode { get; init; }
    public bool HasScriptTags { get; init; }

}