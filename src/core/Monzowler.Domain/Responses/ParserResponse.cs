namespace Monzowler.Crawler.Models;

public class ParserResponse
{
    public List<string?> Links { get; set; }
    public ParserStatusCode StatusCode { get; set; }
}