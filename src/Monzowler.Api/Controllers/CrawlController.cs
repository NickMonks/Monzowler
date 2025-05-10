using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Repository.Interfaces;
using Monzowler.Crawler.Service;
using System;
using System.Threading.Tasks;

namespace Monzowler.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CrawlController : ControllerBase
    {
        private readonly BackgroundCrawlService _crawler;
        private readonly IJobRepository _jobRepository;
        private readonly ISiteMapRepository _siteMapRepository;
        private readonly ILogger<CrawlController> _logger;

        public CrawlController(
            BackgroundCrawlService crawler,
            IJobRepository jobRepository,
            ISiteMapRepository siteMapRepository,
            ILogger<CrawlController> logger)
        {
            _crawler = crawler;
            _jobRepository = jobRepository;
            _siteMapRepository = siteMapRepository;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult EnqueueCrawl([FromBody] CrawlRequest req)
        {
            _logger.LogInformation("Crawl requested for: {Url}", req.Url);
            try
            {
                var rootUrl = new Uri(req.Url).GetLeftPart(UriPartial.Authority).TrimEnd('/');
                var jobId = _crawler.EnqueueCrawl(rootUrl);

                return Accepted($"/crawl/{jobId}", new CrawlResponse
                {
                    JobId = jobId,
                    Status = JobStatus.Created
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to enqueue crawl job and start it for {Url}", req.Url);
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
                var job = await _jobRepository.GetAsync(jobId);
                return job is null
                    ? NotFound(new { Message = $"Job '{jobId}' not found." })
                    : Ok(job);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to fetch crawl job {JobId}", jobId);
                return Problem(
                    title: "Failed to retrieve crawl job",
                    detail: e.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        [HttpGet("sitemap/{jobId}")]
        public async Task<IActionResult> GetSiteMap(string jobId)
        {
            var map = await _siteMapRepository.GetCrawlsByJobIdAsync(jobId);
            return map is null ? NotFound() : Ok(map);
        }
    }
}
