using Monzowler.Crawler.Models;
using Monzowler.Crawler.Repository.Models;

namespace Monzowler.Crawler.Repository.Interfaces;

public interface ISiteMapRepository
{
    public Task SaveCrawlAsync(List<Page> pages);
    public Task<List<CrawlerDbModel>> GetCrawlsByDomainAsync(string domain);
    public Task<List<CrawlerDbModel>> GetCrawlsByJobIdAsync(string jobId);
}