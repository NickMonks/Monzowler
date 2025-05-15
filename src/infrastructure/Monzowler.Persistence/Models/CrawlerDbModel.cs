using Monzowler.Domain.Entities;

namespace Monzowler.Persistence.Models;

public record CrawlerDbModel
{
    public string Domain { get; init; } = null!;
    public string PageUrl { get; init; } = null!;
    public int Depth { get; init; }
    public List<string> Links { get; init; } = null!;
    public string? Status { get; init; }
    public string LastModified { get; init; } = null!;
    public string? JobId { get; init; }

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

    public static Page From(CrawlerDbModel model)
    {
        return new Page
        {
            Domain = model.Domain,
            PageUrl = model.PageUrl,
            Depth = model.Depth,
            Links = model.Links,
            Status = model.Status,
            LastModified = model.LastModified,
            JobId = model.JobId
        };
    }

}