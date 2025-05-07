using HtmlAgilityPack;
using Polly;
using Polly.Retry;
using System.Net.Http;
using Microsoft.Extensions.Configuration;

public class HtmlParser {
    private readonly HttpClient _http;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public HtmlParser(IConfiguration config) {
        // initialize HttpClient with default headers
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(config["UserAgent"]);

        // configure exponential backoff retry: 3 attempts, delay doubles
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, retryAttempt, context) => {
                    // TODO: use logging to record retry attempts
                }
            );
    }

    public async Task<List<string?>> ParseLinksAsync(string pageUrl, string allowedHost) {
        try {
            // fetch with retries
            var response = await _retryPolicy.ExecuteAsync(() => _http.GetAsync(pageUrl));
            if (!response.IsSuccessStatusCode) {
                // TODO: log failure and skip
                return new List<string>();
            }

            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // basic parsing of <a> tags; may fail for JS-heavy sites
            var nodes = doc.DocumentNode.SelectNodes("//a[@href]");
            if (nodes == null) return new List<string>();

            return nodes
                .Select(a => a.GetAttributeValue("href", string.Empty))
                .Select(href => {
                    try {
                        var candidate = new Uri(new Uri(pageUrl), href).ToString().TrimEnd('/');
                        return candidate;
                    } catch {
                        return null;
                    }
                })
                .Where(u => u is not null && new Uri(u).Host == allowedHost)
                .Distinct()
                .ToList();
        } catch {
            // network or parsing failure
            // TODO: consider using a headless browser (e.g. PlaywrightSharp) for JS-rendered pages
            return new List<string>();
        }
    }
}