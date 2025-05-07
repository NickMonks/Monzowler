namespace Monzowler.Crawler.Interfaces;

public interface ICrawlerService
{
    Task<Dictionary<string, List<string>>> CrawlAsync(string rootUrl, int maxDepth);
}