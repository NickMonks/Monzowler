using Microsoft.Extensions.Logging;
using Monzowler.HttpClient.Throttler;

namespace Monzowler.HttpClient.ApiClient;

public interface IApiClient
{
    Task<string> GetStringAsync(string url, CancellationToken ct);
}

public class ApiClient(System.Net.Http.HttpClient httpClient, ILogger<ApiClient> logger, PolitenessThrottler throttler) : IApiClient
{
    private readonly System.Net.Http.HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger<ApiClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly PolitenessThrottler _throttler = throttler ?? throw new ArgumentNullException(nameof(throttler));

    public async Task<string> GetStringAsync(string url, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Sending GET request to {Url}", url);

            // Ensure we are following politeness policy from robots.txt
            var domain = new Uri(url).Host;
            await _throttler.EnforceAsync(domain, ct);

            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Request to {Url} failed with status code {StatusCode}", url, response.StatusCode);
                response.EnsureSuccessStatusCode();
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return content;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Request to {Url} was cancelled.", url);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching {Url}", url);
            throw;
        }
    }
}