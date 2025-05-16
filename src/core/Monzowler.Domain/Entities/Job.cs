using Monzowler.Crawler.Models;

namespace Monzowler.Domain.Entities;

public class Job
{
    public string JobId { get; set; } = default!;
    public JobStatus Status { get; set; }
    public string Url { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
}