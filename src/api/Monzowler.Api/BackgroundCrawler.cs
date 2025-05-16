using System.Diagnostics;
using Monzowler.Application.Contracts.Persistence;
using Monzowler.Application.Contracts.Services;
using Monzowler.Domain.Entities;
using Monzowler.Domain.Requests;
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
    public string EnqueueCrawl(CrawlRequest request)
    {
        var job = new Job
        {
            JobId = Guid.NewGuid().ToString(),
            Status = JobStatus.InProgress,
            Url = request.Url,
            StartedAt = DateTime.UtcNow
        };

        using var span = TracingHelper.StartSpan("JobCreated", job);
        var parentContext = Activity.Current;

        Task.Run(async () =>
        {
            //Important: we need to get the parent context. the span is async-local but not thread-local,
            //So the context might be lost. So we need to grab it from the parent. 
            Activity.Current = parentContext;
            var spanWithActivity = TracingHelper.StartSpanWithActivity("JobStarted", job);

            try
            {
                logger.LogInformation(" ----- JOB {JobId} : START -------", job.JobId);
                await jobRepository.CreateAsync(job);
                var crawlParams = new CrawlParameters
                {
                    JobId = job.JobId,
                    RootUrl = request.Url,
                    MaxDepth = request.MaxDepth,
                    MaxRetries = request.MaxRetries,
                };
                var _ = await spider.CrawlAsync(crawlParams);

                logger.LogInformation("----- JOB {JobId} : COMPLETED -------", job.JobId);
                Activity.Current = parentContext;
                spanWithActivity?.AddEvent(new ActivityEvent("JobCompleted"));

                await jobRepository.UpdateStatusAsync(job.JobId, JobStatus.Completed, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogError("----- JOB {JobId} : FAILED -------", job.JobId);
                Activity.Current = parentContext;
                spanWithActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                spanWithActivity?.AddEvent(new ActivityEvent("JobFailed"));

                await jobRepository.MarkAsFailedAsync(job.JobId, ex.Message);
            }
            finally
            {
                spanWithActivity?.Dispose();
            }
        });

        return job.JobId;
    }
}
