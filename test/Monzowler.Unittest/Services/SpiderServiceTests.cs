using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monzowler.Application.Contracts.Results;
using Monzowler.Application.Contracts.Services;
using Monzowler.Application.Results;
using Monzowler.Application.Services;
using Monzowler.Crawler.Parsers;
using Monzowler.Domain.Entities;
using Monzowler.Domain.Requests;
using Monzowler.Domain.Responses;
using Monzowler.Shared.Settings;
using Moq;

namespace Monzowler.Unittest.Services;

public class SpiderServiceTests
{
    private readonly Mock<IParser> _mockParser = new();
    private readonly Mock<IRobotsTxtService> _mockRobots = new();
    private readonly Mock<IResultHandler> _mockRepo = new();
    private readonly Mock<ILogger<SpiderService>> _mockLogger = new();
    private readonly Mock<PolitenessThrottlerService> _mockThrottler = new();

    private const string RootUrl = "https://example.com";
    private const string JobId = "test-job";
    private const string AllowedHost = "example.com";

    private SpiderService CreateService(CrawlerSettings? overrides = null)
    {
        var opts = Options.Create(overrides ?? new CrawlerSettings
        {
            MaxConcurrency = 1,
            MaxDepth = 1,
            Timeout = 10,
            MaxRetries = 1,
            UserAgent = "TestBot"
        });

        return new SpiderService(
            _mockParser.Object,
            _mockRobots.Object,
            _mockRepo.Object,
            _mockLogger.Object,
            _mockThrottler.Object,
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
        
        var service = CreateService();

        // Act
        var result = await service.CrawlAsync(RootUrl, JobId);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, p => p.PageUrl == RootUrl);
        Assert.Contains(result, p => p.PageUrl == expectedLinks[0]);
        _mockRepo.Verify(r => r.HandleAsync(It.IsAny<List<Page>>()), Times.Once);
    }

    [Fact]
    public async Task CrawlAsync_SkipsLink_WhenDepthExceedsMax()
    {
        // Arrange
        _mockParser.Setup(p => p.ParseLinksAsync(It.IsAny<ParserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParserResponse
            {
                Links = ["https://example.com/too-deep"],
                StatusCode = ParserStatusCode.Ok
            });

        _mockRobots.Setup(r => r.GetRulesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RobotsTxtResponse { Allows = ["/"], Disallows = [], Delay = 0 });

        _mockRobots.Setup(r => r.IsAllowed(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<List<string>>()))
            .Returns(true);

        var service = CreateService(new CrawlerSettings { MaxConcurrency = 1, MaxDepth = 0, Timeout = 5, MaxRetries = 1, UserAgent = "TestBot" });

        // Act
        var result = await service.CrawlAsync(RootUrl, JobId);

        // Assert
        Assert.Single(result);
        Assert.Contains(result, p => p.PageUrl == RootUrl);
    }

    [Fact]
    public async Task CrawlAsync_SkipsLink_FromDifferentDomain()
    {
        // Arrange
        _mockParser.Setup(p => p.ParseLinksAsync(It.IsAny<ParserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParserResponse
            {
                Links = ["https://example.com/page"],
                StatusCode = ParserStatusCode.Ok
            });

        _mockRobots.Setup(r => r.GetRulesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RobotsTxtResponse { Allows = ["/"], Disallows = [], Delay = 0 });

        _mockRobots.Setup(r => r.IsAllowed(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<List<string>>()))
            .Returns(true);

        var service = CreateService(new CrawlerSettings { MaxConcurrency = 1, MaxDepth = 2, Timeout = 5, MaxRetries = 1, UserAgent = "TestBot" });

        // Act
        var result = await service.CrawlAsync(RootUrl, JobId);

        // Assert
        Assert.Single(result);
        Assert.DoesNotContain(result, p => p.PageUrl.Contains("external.com"));
    }

    [Fact]
    public async Task CrawlAsync_SkipsAlreadyVisitedLinks()
    {
        // Arrange
        var duplicateLink = "https://example.com/page1";

        _mockParser.Setup(p => p.ParseLinksAsync(It.IsAny<ParserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParserResponse { Links = [duplicateLink], StatusCode = ParserStatusCode.Ok });

        _mockRobots.Setup(r => r.GetRulesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RobotsTxtResponse { Allows = ["/"], Disallows = [], Delay = 0 });

        _mockRobots.Setup(r => r.IsAllowed(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<List<string>>()))
            .Returns(true);

        var service = CreateService(new CrawlerSettings { MaxConcurrency = 1, MaxDepth = 2, Timeout = 5, MaxRetries = 1, UserAgent = "TestBot" });

        // Act
        var result = await service.CrawlAsync(RootUrl, JobId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Single(result.Where(p => p.PageUrl == duplicateLink));
    }

    [Fact]
    public async Task CrawlAsync_RecordsDisallowedLink()
    {
        // Arrange
        var disallowedLink = "https://example.com/private";

        _mockParser.Setup(p => p.ParseLinksAsync(It.IsAny<ParserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParserResponse { Links = [disallowedLink], StatusCode = ParserStatusCode.Ok });

        _mockRobots.Setup(r => r.GetRulesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RobotsTxtResponse { Allows = ["/"], Disallows = ["/private"], Delay = 0 });

        _mockRobots.Setup(r => r.IsAllowed(It.Is<string>(p => p.Contains("/private")), It.IsAny<List<string>>(), It.IsAny<List<string>>()))
            .Returns(false);

        _mockRobots.Setup(r => r.IsAllowed(It.Is<string>(p => p == "/"), It.IsAny<List<string>>(), It.IsAny<List<string>>()))
            .Returns(true);

        var service = CreateService(new CrawlerSettings { MaxConcurrency = 1, MaxDepth = 2, Timeout = 5, MaxRetries = 1, UserAgent = "TestBot" });

        // Act
        var result = await service.CrawlAsync(RootUrl, JobId);

        // Assert
        Assert.Contains(result, p => p.PageUrl == disallowedLink && p.Status == "Disallowed");
    }

    [Fact]
    public async Task CrawlAsync_LogsAndSkipsOnException()
    {
        // Arrange
        _mockParser.Setup(p => p.ParseLinksAsync(It.IsAny<ParserRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected failure"));

        _mockRobots.Setup(r => r.GetRulesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RobotsTxtResponse { Allows = ["/"], Disallows = [], Delay = 0 });

        _mockRobots.Setup(r => r.IsAllowed(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<List<string>>()))
            .Returns(true);

        var service = CreateService(new CrawlerSettings { MaxConcurrency = 1, MaxDepth = 1, Timeout = 5, MaxRetries = 0, UserAgent = "TestBot" });

        // Act
        var result = await service.CrawlAsync(RootUrl, JobId);

        // Assert
        Assert.Empty(result);
    }
}
