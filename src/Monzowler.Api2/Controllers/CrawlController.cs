using Microsoft.AspNetCore.Mvc;
using Monzowler.Api;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Repository.Interfaces;

namespace Monzowler.Api2.Controllers;

[ApiController]
[Route("[controller]")]
public class CrawlController(
    BackgroundCrawlService crawler,
    IJobRepository jobRepository,
    ISiteMapRepository siteMapRepository,
    ILogger<CrawlController> logger)
    : ControllerBase
{
    [HttpPost]
    public IActionResult PostCrawl([FromBody] CrawlRequest req)
    {
        logger.LogInformation("Crawl requested for: {Url}", req.Url);

        try
        {
            var rootUrl = new Uri(req.Url).GetLeftPart(UriPartial.Authority).TrimEnd('/');
            var jobId = crawler.EnqueueCrawl(rootUrl);
            return Accepted($"/crawl/{jobId}", new CrawlResponse { JobId = jobId, Status = JobStatus.Created });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to enqueue crawl job for {Url}", req.Url);
            return Problem(
                title: "Failed to start crawl job",
                detail: e.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{jobId}")]
    public async Task<IActionResult> GetJob(string jobId)
    {
        try
        {
            var job = await jobRepository.GetAsync(jobId);
            if (job is null)
            {
                return NotFound(new { Message = $"Job '{jobId}' not found." });
            }

            return Ok(job);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to fetch crawl job {JobId}", jobId);
            return Problem(
                title: "Failed to retrieve crawl job",
                detail: e.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("/sitemap/{jobId}")]
    public async Task<IActionResult> GetSitemap(string jobId)
    {
        var map = await siteMapRepository.GetCrawlsByJobIdAsync(jobId);
        return map is null ? NotFound() : Ok(map);
    }
}
