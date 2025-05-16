using Microsoft.Extensions.Logging;
using Monzowler.Api;
using Monzowler.Application.Contracts.Persistence;
using Monzowler.Application.Contracts.Services;
using Monzowler.Domain.Entities;
using Monzowler.Domain.Requests;
using Moq;

namespace Monzowler.Unittest
{
    public class BackgroundCrawlerTests
    {
        [Fact]
        public async Task EnqueueCrawl_ShouldCreateJobAndRunBackgroundTask()
        {
            // Arrange
            var mockSpider = new Mock<ISpiderService>();
            var mockLogger = new Mock<ILogger<BackgroundCrawler>>();
            var mockJobRepository = new Mock<IJobRepository>();

            mockSpider.Setup(s => s.CrawlAsync(It.IsAny<CrawlParameters>()))
                      .ReturnsAsync(new List<Page>());

            var backgroundCrawler = new BackgroundCrawler(mockSpider.Object, mockLogger.Object, mockJobRepository.Object);
            var testUrl = "https://example.com";
            var crawlRequest = new CrawlRequest
            {
                Url = testUrl,
                MaxDepth = 1,
                MaxRetries = 1
            };


            // Act
            var jobId = backgroundCrawler.EnqueueCrawl(crawlRequest);

            // Assert
            var expectedCrawlParams = new CrawlParameters
            {
                RootUrl = testUrl,
                MaxDepth = 1,
                MaxRetries = 1,
                JobId = jobId
            };
            Assert.False(string.IsNullOrWhiteSpace(jobId));
            mockJobRepository.Verify(r => r.CreateAsync(It.Is<Job>(j => j.JobId == jobId && j.Url == testUrl)), Times.Once);

            // Wait a bit to let the background task run
            await Task.Delay(500);

            mockSpider.Verify(s => s.CrawlAsync(It.Is<CrawlParameters>(p =>
                p.RootUrl == expectedCrawlParams.RootUrl &&
                p.MaxDepth == expectedCrawlParams.MaxDepth &&
                p.MaxRetries == expectedCrawlParams.MaxRetries &&
                p.JobId == expectedCrawlParams.JobId
            )), Times.Once);
            mockJobRepository.Verify(r => r.UpdateStatusAsync(jobId, JobStatus.Completed, It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task EnqueueCrawl_ShouldMarkJobAsFailed_WhenExceptionOccurs()
        {
            // Arrange
            var mockSpider = new Mock<ISpiderService>();
            var mockLogger = new Mock<ILogger<BackgroundCrawler>>();
            var mockJobRepository = new Mock<IJobRepository>();

            mockSpider.Setup(s => s.CrawlAsync(It.IsAny<CrawlParameters>()))
                      .ThrowsAsync(new Exception("Spider failure"));

            var backgroundCrawler = new BackgroundCrawler(mockSpider.Object, mockLogger.Object, mockJobRepository.Object);
            var testUrl = "https://example.com";
            var crawlRequest = new CrawlRequest
            {
                Url = testUrl,
                MaxDepth = 1,
                MaxRetries = 1
            };

            // Act
            var jobId = backgroundCrawler.EnqueueCrawl(crawlRequest);

            // Wait a bit for the background task to run
            await Task.Delay(100);

            // Assert
            var expectedCrawlParams = new CrawlParameters
            {
                RootUrl = testUrl,
                MaxDepth = 1,
                MaxRetries = 1,
                JobId = jobId
            };
            mockJobRepository.Verify(r => r.CreateAsync(It.Is<Job>(j => j.JobId == jobId)), Times.Once);
            mockSpider.Verify(s => s.CrawlAsync(It.Is<CrawlParameters>(p =>
                p.RootUrl == expectedCrawlParams.RootUrl &&
                p.MaxDepth == expectedCrawlParams.MaxDepth &&
                p.MaxRetries == expectedCrawlParams.MaxRetries &&
                p.JobId == expectedCrawlParams.JobId
            )), Times.Once);
            mockJobRepository.Verify(r => r.MarkAsFailedAsync(jobId, It.Is<string>(msg => msg.Contains("Spider failure"))), Times.Once);
        }
    }
}
