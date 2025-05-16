using Microsoft.AspNetCore.Mvc;
using Monzowler.Application.Contracts.Persistence;
using Monzowler.Crawler.Models;
using Monzowler.Domain.Entities;
using Monzowler.Domain.Requests;
using Monzowler.Domain.Responses;
using Monzowler.Persistence.Interfaces;

namespace Monzowler.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CrawlController(
        BackgroundCrawler crawler,
        IJobRepository jobRepository,
        ISiteMapRepository siteMapRepository,
        ILogger<CrawlController> logger)
        : ControllerBase
    {
        [HttpPost]
        public IActionResult EnqueueCrawl([FromBody] CrawlRequest req)
        {
            logger.LogInformation("Crawl requested for: {Url}", req.Url);
            try
            {
                var url = new Uri(req.Url).ToString().TrimEnd('/');
                var jobId = crawler.EnqueueCrawl(req);

                return Accepted($"/crawl/{jobId}", new CrawlResponse
                {
                    JobId = jobId,
                    Status = JobStatus.Created
                });
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to enqueue crawl job and start it for {Url}", req.Url);
                return Problem(
                    title: "Failed to start crawl job",
                    detail: e.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        [HttpGet("{jobId}")]
        public async Task<IActionResult> GetJob(string jobId)
        {
            try
            {
                var job = await jobRepository.GetAsync(jobId);
                return job is null
                    ? NotFound(new { Message = $"Job '{jobId}' not found." })
                    : Ok(job);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to fetch crawl job {JobId}", jobId);
                return Problem(
                    title: "Failed to retrieve crawl job",
                    detail: e.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        [HttpGet("sitemap/{jobId}")]
        public async Task<IActionResult> GetSiteMap(
            string jobId,
            [FromQuery] List<ParserStatusCode> status)
        {
            var allCrawls = await siteMapRepository.GetCrawlsByJobIdAsync(jobId);

            //For each status requested we will filter the response
            if (status is { Count: > 0 })
            {
                allCrawls = allCrawls
                    .Where(c =>
                        Enum.TryParse<ParserStatusCode>(c.Status, true, out var parsedStatus) &&
                        status.Contains(parsedStatus))
                    .ToList();
            }

            return allCrawls.Count == 0 ? NoContent() : Ok(allCrawls);
        }
    }
}
