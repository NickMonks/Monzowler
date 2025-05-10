using Monzowler.Crawler.Models;

namespace Monzowler.Crawler.Repository.Models;

public record CrawlerDbModel
{
    public string Domain { get; set; }
    public string PageUrl { get; set; }
    public int Depth { get; set; }
    public List<string> Links { get; set; }
    public string? Status { get; set; }
    public string LastModified { get; set; }
    public string? JobId { get; set; }

    public static CrawlerDbModel To(Page page)
    {
        return new CrawlerDbModel
        {
            Domain = page.Domain,
            PageUrl = page.PageUrl,
            Depth = page.Depth,
            Links = page.Links,
            Status = page.Status,
            LastModified = page.LastModified,
            JobId = page.JobId
        };
    }
}