namespace Monzowler.Crawler.Settings;

public class CrawlerOptions
{
    public int MaxConcurrency { get; set; } = 10;
    public int MaxDepth { get; set; } = 3;
    public int MaxRetries { get; set; } = 3;
}