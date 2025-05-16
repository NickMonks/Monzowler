using Monzowler.Domain.Entities;

namespace Monzowler.Persistence.Models;

public class JobDbModel
{
    public string JobId { get; private init; } = null!;
    public JobStatus Status { get; private init; }
    public string Url { get; private init; } = null!;
    public DateTime? StartedAt { get; private init; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }

    public static JobDbModel To(Job job)
    {
        return new JobDbModel
        {
            JobId = job.JobId,
            Status = job.Status,
            Url = job.Url,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            Error = job.JobId
        };
    }
}