namespace Monzowler.Domain.Requests;

public class CrawlRequest
{
    public required string Url { get; init; }
    public int MaxDepth { get; init; } = 1;
    public int MaxRetries { get; init; } = 2;
}