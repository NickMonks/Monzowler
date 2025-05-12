using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monzowler.Application.Contracts.Services;
using Monzowler.Application.Services;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Parsers;
using Monzowler.Crawler.Settings;
using Monzowler.Domain.Entities;
using Monzowler.Domain.Responses;
using Monzowler.Persistence.Interfaces;
using Moq;

namespace Monzowler.Unittest.Services;

public class SpiderServiceTests
{
    private readonly SpiderService _service;
    private readonly Mock<IParser> _mockParser;
    private readonly Mock<IRobotsTxtService> _mockRobots;
    private readonly Mock<ISiteMapRepository> _mockRepo;

    public SpiderServiceTests()
    {
        _mockParser = new Mock<IParser>();
        _mockRobots = new Mock<IRobotsTxtService>();
        _mockRepo = new Mock<ISiteMapRepository>();
        var mockLogger = new Mock<ILogger<SpiderService>>();
        var mockThrottler = new Mock<PolitenessThrottlerService>();

        var opts = Options.Create(new CrawlerSettings
        {
            MaxConcurrency = 1,
            MaxDepth = 1,
            Timeout = 10,
            MaxRetries = 1,
            UserAgent = "TestBot"
        });

        _service = new SpiderService(
            _mockParser.Object,
            _mockRobots.Object,
            _mockRepo.Object,
            mockLogger.Object,
            mockThrottler.Object,
            opts
        );
    }

    [Fact]
    public async Task CrawlAsync_CrawlsPage_AndSavesToRepository()
    {
        // Arrange
        var expectedLinks = new List<string> { "https://example.com/page2" };

        _mockRobots
            .Setup(r => r.GetRulesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RobotsTxtResponse { Allows = ["/"], Disallows = [], Delay = 0 });

        _mockRobots
            .Setup(r => r.IsAllowed(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<List<string>>()))
            .Returns(true);

        _mockParser
            .Setup(p => p.ParseLinksAsync(It.IsAny<ParserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParserResponse
            {
                Links = expectedLinks,
                StatusCode = ParserStatusCode.Ok
            });

        // Act
        var result = await _service.CrawlAsync("https://example.com", "test-job");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, p => p.PageUrl == "https://example.com");
        Assert.Contains(result, p => p.PageUrl == "https://example.com/page2");

        _mockRepo.Verify(r => r.SaveCrawlAsync(It.IsAny<List<Page>>()), Times.Once);
    }
}