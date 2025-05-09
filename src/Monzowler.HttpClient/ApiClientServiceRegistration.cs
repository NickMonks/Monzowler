using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monzowler.Crawler.Settings;
using Monzowler.HttpClient.ApiClient;
using Monzowler.HttpClient.Handlers;
using Monzowler.HttpClient.Throttler;
using Polly;
using Polly.Extensions.Http;

namespace Monzowler.HttpClient;

public static class ApiClientServiceRegistration
{
    public static void AddApiClientServices(this IServiceCollection services, IConfiguration configuration)
    {
        //TODO: consider adding exception handler for different errors 
        //.AddHttpMessageHandler<ApiExceptionHandler>();

        services.AddSingleton<PolitenessThrottler>();
        services.AddHttpClient<IApiClient, ApiClient.ApiClient>(client =>
            {
                var httpApiSettings = configuration
                    .GetSection("ApiClient")
                    .Get<ApiClientOptions>() ?? throw new NullReferenceException();
                
                client.DefaultRequestHeaders.UserAgent.ParseAdd(httpApiSettings.UserAgent);
            })
            .AddPolicyHandler((sp, request) =>
            {
                var throttler = sp.GetRequiredService<PolitenessThrottler>();

                // Extract the domain from the request URL
                var domain = request.RequestUri?.Host ?? "default";

                return GetRetryPolicy(throttler, domain);
            });
    }
    
    /// <summary>
    /// Retry policy for our HTTP Client. We will retry on 5xx, 408 (Timeout) errors.
    /// The current configuration has 5 retires with an exponential backoff strategy.
    /// </summary>
    /// <returns></returns>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(PolitenessThrottler throttler, string domain)
    {
        //TODO: potentially retry 429s with the after-retry header 
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 5, 
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetryAsync: async (outcome, timespan, retryAttempt, context) =>
                {
                    await throttler.EnforceAsync(domain);
                    Console.WriteLine($"Retry {retryAttempt} after politeness + {timespan}");
                });
    }

}