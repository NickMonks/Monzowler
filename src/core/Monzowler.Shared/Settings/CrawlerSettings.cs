namespace Monzowler.Crawler.Settings;

public class CrawlerSettings
{
    public string UserAgent { get; set; }
    public int MaxConcurrency { get; set; }
    public int MaxDepth { get; set; }
    public int MaxRetries { get; set; }

    public int Timeout { get; set; }


}