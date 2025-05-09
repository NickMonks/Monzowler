using System.Collections.Concurrent;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Repository.Models;

namespace Monzowler.Crawler.Interfaces;

public interface ISiteMapRepository
{
    public Task SaveCrawlAsync(List<Page> pages);
    public Task<List<CrawlerDbModel>> GetCrawlsByDomainAsync(string domain);
}