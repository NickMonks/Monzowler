using Microsoft.Extensions.Logging;
using Monzowler.Application.Contracts.HttpClient;
using Monzowler.Application.Services;
using Monzowler.Unittest.Helpers;
using Moq;

namespace Monzowler.Unittest.Services;

public class RobotsTxtServiceTests
{
    private readonly Mock<IApiClient> _mockApiClient;
    private readonly Mock<ILogger<RobotsTxtService>> _mockLogger;
    private readonly RobotsTxtService _service;

    public RobotsTxtServiceTests()
    {
        _mockApiClient = new Mock<IApiClient>();
        _mockLogger = new Mock<ILogger<RobotsTxtService>>();
        _service = new RobotsTxtService(_mockApiClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetRulesAsync_ReturnsCorrectRules_ForMatchingUserAgent()
    {
        //Arrange 
        var content = Helper.LoadTestFile("valid_robots.txt");
        _mockApiClient
            .Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        //Act
        var result = await _service.GetRulesAsync("https://example.com", "TestBot");

        //Assert
        Assert.Contains("/private", result.Disallows);
        Assert.Contains("/private/allowed", result.Allows);
        Assert.Equal(5000, result.Delay);
    }

    [Fact]
    public async Task GetRulesAsync_ReturnsCorrectRules_ForMatchingUserAgent_WhenMultipleAgentsInRobots()
    {
        //Arrange 
        var content = Helper.LoadTestFile("multiple_agents_robots.txt");
        _mockApiClient
            .Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        //Act
        var result = await _service.GetRulesAsync("https://example.com", "TestBot2");

        //Assert
        Assert.Contains("/private2", result.Disallows);
        Assert.Contains("/private2/allowed", result.Allows);
        Assert.Equal(2000, result.Delay);
    }

    [Fact]
    public async Task GetRulesAsync_FallsBackToWildcardGroup_WhenAgentNotFound()
    {
        //Arrange 
        var content = Helper.LoadTestFile("valid_robots.txt");
        _mockApiClient
            .Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        //Act 
        var result = await _service.GetRulesAsync("https://example.com", "UnknownBot");

        //Assert
        Assert.Contains("/tmp", result.Disallows);
        Assert.Empty(result.Allows);
        Assert.Equal(1000, result.Delay);
    }

    [Fact]
    public async Task GetRulesAsync_ReturnsEmptyRules_OnHttpError()
    {
        //Arrange
        _mockApiClient
            .Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Simulated failure"));

        //Act
        var result = await _service.GetRulesAsync("https://example.com", "TestBot");

        //Assert
        Assert.Empty(result.Disallows);
        Assert.Empty(result.Allows);
        Assert.Equal(0, result.Delay);
    }

    [Fact]
    public async Task GetRulesAsync_IgnoresRulesWithoutUserAgent()
    {
        //Arrange
        var content = Helper.LoadTestFile("malformed_no_useragent.txt");

        _mockApiClient
            .Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        //Act
        var result = await _service.GetRulesAsync("https://example.com", "TestBot");

        //Assert
        Assert.Empty(result.Disallows);
        Assert.Empty(result.Allows);
    }

    [Fact]
    public async Task GetRulesAsync_IgnoresUnknownDirectives()
    {
        //Arrange
        var content = Helper.LoadTestFile("malformed_invalid_directive.txt");

        _mockApiClient
            .Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        //Act
        var result = await _service.GetRulesAsync("https://example.com", "TestBot");

        //Assert
        Assert.Contains("/bad", result.Disallows);
        Assert.Empty(result.Allows);
    }

    [Fact]
    public async Task GetRulesAsync_UsesOnlyFirstGroupForSameUserAgent()
    {
        //Arrange
        var content = Helper.LoadTestFile("valid_same_agent_robots.txt");

        _mockApiClient
            .Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        //Act
        var result = await _service.GetRulesAsync("https://example.com", "TestBot");

        //Assert
        Assert.Contains("/first-group", result.Disallows);
        Assert.DoesNotContain("/second-group", result.Disallows);
        Assert.DoesNotContain("/allowed-later", result.Allows);
    }

    [Theory]
    [InlineData("/private/allowed", true)]
    [InlineData("/private/blocked", false)]
    [InlineData("/public", true)]
    public void IsAllowed_ReturnsCorrectValue(string path, bool expected)
    {
        //Arrange
        var allows = new List<string> { "/private/allowed" };
        var disallows = new List<string> { "/private" };

        //Act
        var result = _service.IsAllowed(path, disallows, allows);

        //Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(new[] { "TestBot" }, "TestBot", true)]
    [InlineData(new[] { "testbot" }, "TESTBOT", true)]
    [InlineData(new[] { "*" }, "AnyBot", true)]
    [InlineData(new[] { "Googlebot" }, "Bingbot", false)]
    [InlineData(new[] { "Bot1", "TestBot" }, "TestBot", true)]
    [InlineData(new[] { "Bot1", "Bot2" }, "TestBot", false)]
    public void Matches_ReturnsExpectedResult(string[] agents, string userAgent, bool expected)
    {
        var group = new RobotsTxtService.RobotsGroup();
        group.Agents.AddRange(agents);

        var result = group.Matches(userAgent);

        Assert.Equal(expected, result);
    }
}