using Microsoft.Extensions.Logging;
using Monzowler.Api;
using Monzowler.Application.Contracts.Persistence;
using Monzowler.Crawler.Interfaces;
using Monzowler.Crawler.Models;
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

            mockSpider.Setup(s => s.CrawlAsync(It.IsAny<string>(), It.IsAny<string>()))
                      .ReturnsAsync(new List<Page>());

            var backgroundCrawler = new BackgroundCrawler(mockSpider.Object, mockLogger.Object, mockJobRepository.Object);
            var testUrl = "https://example.com";

            // Act
            var jobId = backgroundCrawler.EnqueueCrawl(testUrl);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(jobId));
            mockJobRepository.Verify(r => r.CreateAsync(It.Is<Job>(j => j.JobId == jobId && j.Url == testUrl)), Times.Once);

            // Wait a bit to let the background task run (if it fails silently, we want it captured)
            await Task.Delay(100);

            mockSpider.Verify(s => s.CrawlAsync(testUrl, jobId), Times.Once);
            mockJobRepository.Verify(r => r.UpdateStatusAsync(jobId, JobStatus.Completed, It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task EnqueueCrawl_ShouldMarkJobAsFailed_WhenExceptionOccurs()
        {
            // Arrange
            var mockSpider = new Mock<ISpiderService>();
            var mockLogger = new Mock<ILogger<BackgroundCrawler>>();
            var mockJobRepository = new Mock<IJobRepository>();

            mockSpider.Setup(s => s.CrawlAsync(It.IsAny<string>(), It.IsAny<string>()))
                      .ThrowsAsync(new Exception("Spider failure"));

            var backgroundCrawler = new BackgroundCrawler(mockSpider.Object, mockLogger.Object, mockJobRepository.Object);
            var testUrl = "https://example.com";

            // Act
            var jobId = backgroundCrawler.EnqueueCrawl(testUrl);

            // Wait a bit for the background task to run
            await Task.Delay(100);

            // Assert
            mockJobRepository.Verify(r => r.CreateAsync(It.Is<Job>(j => j.JobId == jobId)), Times.Once);
            mockSpider.Verify(s => s.CrawlAsync(testUrl, jobId), Times.Once);
            mockJobRepository.Verify(r => r.MarkAsFailedAsync(jobId, It.Is<string>(msg => msg.Contains("Spider failure"))), Times.Once);
        }
    }
}
