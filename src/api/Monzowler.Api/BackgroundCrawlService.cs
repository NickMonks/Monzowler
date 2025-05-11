using System.Collections.Concurrent;
using System.Diagnostics;
using Monzowler.Application.Contracts.Persistence;
using Monzowler.Crawler.Interfaces;
using Monzowler.Crawler.Models;
using Monzowler.Shared.Observability;
using OpenTelemetry.Trace;

namespace Monzowler.Api;

/// <summary>
/// Due to the nature of the crawler we don't want the user to wait for the whole response of the crawler.
/// Therefore, we run a background worker that process the crawling separately. 
/// </summary>
public class BackgroundCrawlService(
    ISpiderService spider,
    ILogger<BackgroundCrawlService> logger,
    IJobRepository jobRepository)
{
    private readonly ConcurrentDictionary<string, List<Page>> _results = new();

    public string EnqueueCrawl(string url)
    {
        var job = new Job
        {
            JobId = Guid.NewGuid().ToString(),
            Status = JobStatus.InProgress,
            Url = url,
            StartedAt = DateTime.UtcNow
        };
        
        using var span = TracingHelper.StartSpan("JobCreated", job);
        var parentContext = Activity.Current;
        
        Task.Run(async () =>
        {
            Activity.Current = parentContext;
            Activity? span = TracingHelper.StartSpanWithActivity("JobStarted", job);

            try
            {
                logger.LogInformation(" ----- JOB {JobId} : START -------", job.JobId);
        
                await jobRepository.CreateAsync(job);
                var _ = await spider.CrawlAsync(url, job.JobId);

                logger.LogInformation("----- JOB {JobId} : COMPLETED -------", job.JobId);
                span?.AddEvent(new ActivityEvent("JobCompleted"));

                await jobRepository.UpdateStatusAsync(job.JobId, JobStatus.Completed, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogInformation("----- JOB {JobId} : FAILED -------", job.JobId);
                span?.SetStatus(ActivityStatusCode.Error, ex.Message);
                span?.AddEvent(new ActivityEvent("JobFailed"));

                await jobRepository.MarkAsFailedAsync(job.JobId, ex.Message);
            }
            finally
            {
                //Important: we need to dispose this to mark the span as ready to export
                span?.Dispose();
            }
        });
        
        return job.JobId;
    }
}
