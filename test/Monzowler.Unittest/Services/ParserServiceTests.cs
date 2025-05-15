using System.Net;
using Microsoft.Extensions.Logging;
using Monzowler.Application.Services;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Parsers;
using Monzowler.Domain.Entities;
using Monzowler.Domain.Requests;
using Monzowler.Domain.Responses;
using Moq;

namespace Monzowler.Unittest.Services;

public class ParserServiceTests
{
    private readonly Mock<ILogger<ParserService>> _mockLogger = new();
    private static readonly List<string> MockLinks = ["https://example.com/page1"];

    private ParserRequest CreateRequest() => new()
    {
        Url = "https://example.com",
        AllowedHost = "example.com"
    };

    [Fact]
    public async Task ReturnsLinks_WhenFirstParserSucceeds()
    {
        //Arrange
        var parser1 = new Mock<ISubParser>();
        parser1.Setup(p => p.ParseLinksAsync(It.IsAny<ParserRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ParserResponse
               {
                   Links = MockLinks,
                   HasScriptTags = false
               });

        var service = new ParserService([parser1.Object], _mockLogger.Object);

        //Act
        var result = await service.ParseLinksAsync(CreateRequest(), CancellationToken.None);

        //Assert
        Assert.Equal(ParserStatusCode.Ok, result.StatusCode);
        Assert.Single(result.Links);
    }

    [Fact]
    public async Task FallsBackToSecondParser_WhenFirstReturnsNoLinks_ButHasScriptTags()
    {
        //Arrange
        var parser1 = new Mock<ISubParser>();
        var parser2 = new Mock<ISubParser>();
        parser1.Setup(p => p.ParseLinksAsync(It.IsAny<ParserRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ParserResponse { Links = [], HasScriptTags = true });
        parser2.Setup(p => p.ParseLinksAsync(It.IsAny<ParserRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ParserResponse
               {
                   Links = MockLinks,
                   HasScriptTags = false
               });
        var service = new ParserService([parser1.Object, parser2.Object], _mockLogger.Object);

        //Act
        var result = await service.ParseLinksAsync(CreateRequest(), CancellationToken.None);

        //Assert
        Assert.Equal(ParserStatusCode.Ok, result.StatusCode);
        Assert.Single(result.Links);
    }

    [Fact]
    public async Task ReturnsNoLinksFound_WhenParserReturnsNoLinks_AndNoScriptTags()
    {
        //Arrange
        var parser1 = new Mock<ISubParser>();
        parser1.Setup(p => p.ParseLinksAsync(It.IsAny<ParserRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ParserResponse { Links = [], HasScriptTags = false });
        var service = new ParserService([parser1.Object], _mockLogger.Object);

        //Act
        var result = await service.ParseLinksAsync(CreateRequest(), CancellationToken.None);

        //Assert
        Assert.Equal(ParserStatusCode.NoLinksFound, result.StatusCode);
        Assert.Empty(result.Links);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout, ParserStatusCode.TimeoutError)]
    [InlineData(HttpStatusCode.NotFound, ParserStatusCode.NotFoundError)]
    [InlineData(HttpStatusCode.Forbidden, ParserStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError, ParserStatusCode.ServerError)]
    public async Task ReturnsProperStatusCode_OnHttpRequestException(HttpStatusCode statusCode, ParserStatusCode expected)
    {
        //Arrange
        var parser = new Mock<ISubParser>();
        parser.Setup(p => p.ParseLinksAsync(It.IsAny<ParserRequest>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new HttpRequestException("Error", null, statusCode));
        var service = new ParserService([parser.Object], _mockLogger.Object);

        //Act
        var result = await service.ParseLinksAsync(CreateRequest(), CancellationToken.None);

        //Assert
        Assert.Equal(result.StatusCode, expected);
        Assert.Empty(result.Links);
    }

    [Fact]
    public async Task ReturnsParserError_WhenAllParsersThrow()
    {
        //Arrange
        var parser1 = new Mock<ISubParser>();
        var parser2 = new Mock<ISubParser>();

        parser1.Setup(p => p.ParseLinksAsync(It.IsAny<ParserRequest>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new Exception("Failure"));
        parser2.Setup(p => p.ParseLinksAsync(It.IsAny<ParserRequest>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new Exception("Still failing"));

        var service = new ParserService([parser1.Object, parser2.Object], _mockLogger.Object);

        //Act
        var result = await service.ParseLinksAsync(CreateRequest(), CancellationToken.None);

        //Assert
        Assert.Equal(ParserStatusCode.ParserError, result.StatusCode);
        Assert.Empty(result.Links);
    }
}