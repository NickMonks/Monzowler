namespace Monzowler.Crawler.Settings;

public class CrawlerOptions
{
    public int MaxConcurrency { get; set; }
    public int MaxDepth { get; set; }
    public int MaxRetries { get; set; }
    public int Timeout { get; set; }


}