namespace Monzowler.Crawler.Models;

public class CrawlRequest {
    public string Url { get; set; }
    public int MaxDepth { get; set; } = 3;
}