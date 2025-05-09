using Monzowler.Crawler.Models;

namespace Monzowler.Crawler.Repository.Models;

public record CrawlerDbModel
{
    public string Domain {get; set;}
    public string PageUrl {get; set;}
    public List<string> Links {get; set;}
    public string? Error { get; set; }
    public string LastModified {get; set;}
    public string? JobId {get; set;}
    
    public static CrawlerDbModel To(Page page)
    {
        return new CrawlerDbModel
        {
            Domain = page.Domain,
            PageUrl = page.PageUrl,
            Links = page.Links,
            Error = page.Error,
            LastModified = page.LastModified,
            JobId = page.JobId
        };
    }
}