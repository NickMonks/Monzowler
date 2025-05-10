using Monzowler.Crawler.Models;

namespace Monzowler.Crawler.Repository.Models;

public class JobDbModel
{
    public string JobId { get; set; } = default!;
    public JobStatus Status { get; set; }
    public string Url { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
}