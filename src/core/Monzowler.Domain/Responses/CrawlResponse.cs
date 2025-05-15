using Monzowler.Crawler.Models;

namespace Monzowler.Domain.Responses;

public class CrawlResponse
{
    public required string JobId { get; set; }
    public JobStatus Status { get; set; }

}
