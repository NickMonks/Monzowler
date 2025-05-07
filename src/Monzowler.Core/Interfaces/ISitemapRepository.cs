using System.Collections.Concurrent;

namespace Monzowler.Crawler.Interfaces;

public interface ISitemapRepository {
    Task SaveSitemapAsync(string rootUrl, ConcurrentDictionary<string,List<string>> sitemap);
    Task<Dictionary<string,List<string>>> GetSitemapAsync(string rootUrl);
}