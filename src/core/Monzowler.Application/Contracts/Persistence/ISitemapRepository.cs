using Monzowler.Crawler.Models;
using Monzowler.Domain.Entities;

namespace Monzowler.Persistence.Interfaces;

public interface ISiteMapRepository
{
    public Task SaveCrawlAsync(List<Page> pages);
    public Task<List<Page>> GetCrawlsByDomainAsync(string domain);
    public Task<List<Page>> GetCrawlsByJobIdAsync(string jobId);
}