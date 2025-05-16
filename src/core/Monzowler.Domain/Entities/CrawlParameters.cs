namespace Monzowler.Domain.Entities;

public class CrawlParameters
{
    public required string JobId { get; set; }
    public required string RootUrl { get; set; }
    public required int MaxDepth { get; set; }
    public required int MaxRetries { get; set; }
}