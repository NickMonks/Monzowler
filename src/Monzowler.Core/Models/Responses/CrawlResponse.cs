namespace Monzowler.Crawler.Models;

public class CrawlResponse {
    public string Url { get; set; }
    public List<string> Links { get; set; } = new();

    public static CrawlResponse To(List<Page?> pages)
    {
        if (pages == null || pages.Count == 0)
        {
            return new CrawlResponse
            {
                Url = string.Empty,
                Links = new List<string>()
            };
        }

        return new CrawlResponse
        {
            Url = pages[0].PageUrl,
            Links = pages.SelectMany(p => p.Links).Distinct().ToList()
        };
    }
}
