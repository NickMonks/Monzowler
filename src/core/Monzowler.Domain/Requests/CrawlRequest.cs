namespace Monzowler.Domain.Requests;

public class CrawlRequest
{
    public string Url { get; init; }
    public int MaxDepth { get; set; } = 5;

    public int MaxRetries { get; set; } = 1;
}