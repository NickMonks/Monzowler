using Microsoft.Extensions.Logging;
using Monzowler.Application.Parsers;
using Monzowler.Crawler.Contracts.HttpClient;
using Monzowler.Crawler.Models;
using Monzowler.Unittest.Helpers;
using Moq;

namespace Monzowler.Unittest.Parsers;

public class StaticHtmlParserTests
{
    private readonly Mock<IApiClient> _mockHttp;
    private readonly StaticHtmlParser _parser;
    private const string Url = "https://example.com";
    private const string AllowedHost = "example.com";

    public StaticHtmlParserTests()
    {
        _mockHttp = new Mock<IApiClient>();
        var mockLogger = new Mock<ILogger<StaticHtmlParser>>();
        _parser = new StaticHtmlParser(_mockHttp.Object, mockLogger.Object);
    }

    [Fact]
    public async Task ParseLinksAsync_ExtractsValidLinks()
    {
        //Arrange
        var html = Helper.LoadStaticHtlm("valid.html");

        _mockHttp.Setup(h => h.GetStringAsync(Url, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(html);

        var request = new ParserRequest
        {
            Url = Url,
            AllowedHost = AllowedHost
        };

        //Act
        var result = await _parser.ParseLinksAsync(request, CancellationToken.None);

        //Assert
        Assert.Contains("https://example.com/page1", result.Links);
        Assert.Contains("https://example.com/page2", result.Links);
        Assert.DoesNotContain("https://other-example.com/page1", result.Links);
    }

    [Fact]
    public async Task ParseLinksAsync_HasScriptTag_ReturnsTrue()
    {
        //Arrange
        var html = Helper.LoadStaticHtlm("valid_with_script_tags.html");

        _mockHttp.Setup(h => h.GetStringAsync(Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(html);

        var request = new ParserRequest
        {
            Url = Url,
            AllowedHost = AllowedHost
        };

        //Act
        var result = await _parser.ParseLinksAsync(request, CancellationToken.None);

        //Assert
        Assert.True(result.HasScriptTags);
    }


    [Fact]
    public async Task ParseLinksAsync_ReturnsEmptyList_WhenNoLinks()
    {
        //Arrange
        var html = Helper.LoadStaticHtlm("valid_no_links.html");

        _mockHttp.Setup(h => h.GetStringAsync(Url, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(html);

        var request = new ParserRequest
        {
            Url = Url,
            AllowedHost = AllowedHost
        };

        //Arrange
        var result = await _parser.ParseLinksAsync(request, CancellationToken.None);

        //Assert
        Assert.Empty(result.Links);
    }


    [Fact]
    public async Task ParseLinksAsync_RemovesDuplicateLinks()
    {
        //Arrange
        var html = Helper.LoadStaticHtlm("duplicate_links.html");

        _mockHttp.Setup(h => h.GetStringAsync(Url, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(html);

        var request = new ParserRequest
        {
            Url = Url,
            AllowedHost = AllowedHost
        };

        //Act
        var result = await _parser.ParseLinksAsync(request, CancellationToken.None);

        //Assert
        Assert.Single(result.Links);
    }

    [Fact]
    public async Task ParseLinksAsync_ReturnsOnlySanitizedLinks()
    {
        //Arrange
        var html = Helper.LoadStaticHtlm("mixed_links.html");

        _mockHttp.Setup(h => h.GetStringAsync(Url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(html);

        var request = new ParserRequest
        {
            Url = Url,
            AllowedHost = AllowedHost
        };

        //Act
        var result = await _parser.ParseLinksAsync(request, CancellationToken.None);

        // Assert - Should contain only these two valid links
        var expected = new[]
        {
                "https://example.com/page1",
                "https://example.com/page2"
            };

        Assert.Equal(expected.OrderBy(x => x), result.Links.OrderBy(x => x));
    }
}