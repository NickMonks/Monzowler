using System.Net;
using Microsoft.Extensions.Logging;
using Monzowler.Application.Contracts.Services;
using Monzowler.HttpClient;
using Moq;
using Moq.Protected;

namespace Monzowler.Unittest.HttpClient;

public class ApiClientTests
{
    private readonly Mock<IPolitenessThrottlerService> _throttlerMock = new();
    private readonly Mock<ILogger<ApiClient>> _loggerMock = new();

    private static System.Net.Http.HttpClient CreateHttpClient(HttpResponseMessage response, out Mock<HttpMessageHandler> handlerMock)
    {
        handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return new System.Net.Http.HttpClient(handlerMock.Object);
    }

    [Fact]
    public async Task GetStringAsync_ReturnsContent_OnSuccess()
    {
        var expected = "Hello, world!";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expected)
        };

        var client = CreateHttpClient(response, out _);
        var apiClient = new ApiClient(client, _loggerMock.Object, _throttlerMock.Object);

        var result = await apiClient.GetStringAsync("https://example.com", CancellationToken.None);

        Assert.Equal(expected, result);
        _throttlerMock.Verify(t => t.EnforceAsync("example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStringAsync_Throws_OnTimeout()
    {
        var client = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.RequestTimeout), out _);
        var apiClient = new ApiClient(client, _loggerMock.Object, _throttlerMock.Object);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            apiClient.GetStringAsync("https://example.com", CancellationToken.None));

        _throttlerMock.Verify(t => t.EnforceAsync("example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStringAsync_Throws_On5xx()
    {
        var client = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.InternalServerError), out _);
        var apiClient = new ApiClient(client, _loggerMock.Object, _throttlerMock.Object);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            apiClient.GetStringAsync("https://example.com", CancellationToken.None));

        _throttlerMock.Verify(t => t.EnforceAsync("example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStringAsync_Throws_On429()
    {
        var response = new HttpResponseMessage((HttpStatusCode)429);
        response.Headers.Add("Retry-After", "5");

        var client = CreateHttpClient(response, out _);
        var apiClient = new ApiClient(client, _loggerMock.Object, _throttlerMock.Object);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            apiClient.GetStringAsync("https://example.com", CancellationToken.None));

        _throttlerMock.Verify(t => t.EnforceAsync("example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStringAsync_Throws_OnCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var client = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK), out _);
        var apiClient = new ApiClient(client, _loggerMock.Object, _throttlerMock.Object);

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            apiClient.GetStringAsync("https://example.com", cts.Token));

        _throttlerMock.Verify(t => t.EnforceAsync("example.com", It.IsAny<CancellationToken>()), Times.Once);
    }
}