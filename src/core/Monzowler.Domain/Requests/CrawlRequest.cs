namespace Monzowler.Domain.Requests;

public class CrawlRequest
{
    public required string Url { get; init; }
    public int MaxDepth { get; set; } = 1;

    public int MaxRetries { get; set; } = 2;
}