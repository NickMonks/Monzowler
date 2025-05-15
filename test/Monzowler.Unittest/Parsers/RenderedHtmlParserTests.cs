using Microsoft.Extensions.Logging;
using Monzowler.Application.Parsers;
using Monzowler.Application.Services;
using Monzowler.Crawler.Contracts.HttpClient;
using Monzowler.Crawler.Models;
using Monzowler.Domain.Requests;
using Monzowler.Unittest.Helpers;
using Moq;

namespace Monzowler.Unittest.Parsers;

public class RenderedHtmlParserTests : IDisposable
{
    private readonly Mock<IApiClient> _mockHttp;
    private readonly RenderedHtmlParser _parser;
    private readonly BrowserProvider _provider;
    private const string Url = "https://example.com";
    private const string AllowedHost = "example.com";

    public RenderedHtmlParserTests()
    {
        _provider = new BrowserProvider();
        _mockHttp = new Mock<IApiClient>();
        var logger = new Mock<ILogger<RenderedHtmlParser>>();
        _parser = new RenderedHtmlParser(_provider, _mockHttp.Object, logger.Object);
    }

    [Fact]
    public async Task ParseLinksAsync_ExtractsValidLinks()
    {
        var html = Helper.LoadRenderedHtlm("valid.html");

        _mockHttp.Setup(h => h.GetStringAsync(Url, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(html);

        var request = new ParserRequest
        {
            Url = Url,
            AllowedHost = AllowedHost
        };

        var result = await _parser.ParseLinksAsync(request, CancellationToken.None);

        Assert.Contains("https://example.com/page1", result.Links);
        Assert.Contains("https://example.com/page2", result.Links);
        Assert.DoesNotContain("https://other-example.com/page1", result.Links);
    }

    [Fact]
    public async Task ParseLinksAsync_ReturnsEmptyList_WhenNoLinks()
    {
        var html = Helper.LoadRenderedHtlm("valid_no_links.html");

        _mockHttp.Setup(h => h.GetStringAsync(Url, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(html);

        var request = new ParserRequest
        {
            Url = Url,
            AllowedHost = AllowedHost
        };

        var result = await _parser.ParseLinksAsync(request, CancellationToken.None);

        Assert.Empty(result.Links);
    }

    [Fact]
    public async Task ParseLinksAsync_RemovesDuplicateLinks()
    {
        var html = Helper.LoadRenderedHtlm("duplicate_links.html");

        _mockHttp.Setup(h => h.GetStringAsync(Url, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(html);

        var request = new ParserRequest
        {
            Url = Url,
            AllowedHost = AllowedHost
        };

        var result = await _parser.ParseLinksAsync(request, CancellationToken.None);

        Assert.Single(result.Links);
    }

    [Fact]
    public async Task ParseLinksAsync_ReturnsOnlySanitizedLinks()
    {
        var html = Helper.LoadRenderedHtlm("mixed_links.html");

        _mockHttp.Setup(h => h.GetStringAsync(Url, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(html);

        var request = new ParserRequest
        {
            Url = Url,
            AllowedHost = AllowedHost
        };

        var result = await _parser.ParseLinksAsync(request, CancellationToken.None);

        var expected = new[]
        {
            "https://example.com/page1",
            "https://example.com/page2"
        };

        Assert.Equal(expected.OrderBy(x => x), result.Links.OrderBy(x => x));
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}
