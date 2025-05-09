using Monzowler.Crawler.Models;

namespace Monzowler.Crawler.Interfaces;

public interface ISpiderService
{
    Task<List<Page>> CrawlAsync(string rootUrl);
}