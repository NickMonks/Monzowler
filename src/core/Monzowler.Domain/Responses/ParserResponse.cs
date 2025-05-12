using Monzowler.Domain.Entities;

namespace Monzowler.Domain.Responses;

public class ParserResponse
{
    public List<string> Links { get; set; }
    public ParserStatusCode StatusCode { get; set; }
    public bool HasScriptTags { get; set; } = false;

}