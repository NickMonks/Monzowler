using System.Collections.Concurrent;
using System.Diagnostics;
using Monzowler.Application.Contracts.Persistence;
using Monzowler.Crawler.Interfaces;
using Monzowler.Crawler.Models;
using Monzowler.Shared.Observability;

namespace Monzowler.Api;

/// <summary>
/// Due to the nature of the crawler we don't want the user to wait for the whole response of the crawler.
/// Therefore, we run a background task that process the crawling separately. It also tracks the job progress and status
/// </summary>
public class BackgroundCrawler(
    ISpiderService spider,
    ILogger<BackgroundCrawler> logger,
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
            //Important: we need to get the parent context. the span is async-local but not thread-local,
            //So the context might be lost. So we need to grab it from the parent. 
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
                logger.LogError("----- JOB {JobId} : FAILED -------", job.JobId);
                span?.SetStatus(ActivityStatusCode.Error, ex.Message);
                span?.AddEvent(new ActivityEvent("JobFailed"));

                await jobRepository.MarkAsFailedAsync(job.JobId, ex.Message);
            }
            finally
            {
                //Important: we need to dispose this to mark the span as ready to export!
                span?.Dispose();
            }
        });

        return job.JobId;
    }
}
