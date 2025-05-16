using Monzowler.Domain.Entities;

namespace Monzowler.Domain.Responses;

public class CrawlResponse
{
    public required string JobId { get; set; }
    public JobStatus Status { get; set; }

}
