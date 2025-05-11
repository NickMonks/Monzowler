using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monzowler.Application.Services;
using Monzowler.Crawler.Contracts.HttpClient;
using Monzowler.Crawler.Settings;
using Polly;
using Polly.Extensions.Http;

namespace Monzowler.HttpClient;

public static class ApiClientServiceRegistration
{
    public static void AddApiClientRegistration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<PolitenessThrottlerService>();
        services.AddHttpClient<IApiClient, ApiClient.ApiClient>(client =>
            {
                var crawlerSettings = configuration
                    .GetSection("Crawler")
                    .Get<CrawlerSettings>() ?? throw new NullReferenceException();
                
                client.DefaultRequestHeaders.UserAgent.ParseAdd(crawlerSettings.UserAgent);
            })
            .AddPolicyHandler((sp, request) =>
            {
                var throttler = sp.GetRequiredService<PolitenessThrottlerService>();
                var domain = request.RequestUri?.Host ?? "default";

                return GetRetryPolicy(throttler, domain);
            });
    }

    /// <summary>
    /// Retry policy for our HTTP Client. We will retry on 5xx, 408 (Timeout) errors.
    /// The current configuration has 5 retires with an exponential backoff strategy.
    /// Because we are using a webcrawler we need to follow politness from robot txt, and some
    /// pages have a requirement to retry after X seconds. 
    /// </summary>
    /// <returns></returns>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(PolitenessThrottlerService throttlerService, string domain)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 5,
                // The strategy is the following:
                // Check if the error is a 429 TooManyRequest Error - if so we need to honour the
                // retry-after header from rate limiting.
                // Otherwise, we continue with our retry after exponential backoff
                sleepDurationProvider: (retryAttempt, response, context) =>
                {
                    if (response?.Result?.StatusCode == (HttpStatusCode)429 &&
                        response.Result.Headers.TryGetValues("Retry-After", out var values))
                    {
                        var retryAfter = values.FirstOrDefault();
                        //Sometimes this could be a timestamp or seconds, as per RFC 7231
                        //So we need to deal with both
                        if (int.TryParse(retryAfter, out var seconds))
                            return TimeSpan.FromSeconds(seconds);
                        if (DateTimeOffset.TryParse(retryAfter, out var date))
                            return date - DateTimeOffset.UtcNow;
                    }

                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                },
                onRetryAsync: async (_, timespan, retryAttempt, _) =>
                {
                    await throttlerService.EnforceAsync(domain);
                });
    }

}