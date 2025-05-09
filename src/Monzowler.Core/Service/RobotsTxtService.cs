using Microsoft.Extensions.Logging;
using Monzowler.Crawler.Models;
using Monzowler.HttpClient.ApiClient;

namespace Monzowler.Crawler.Service;

/// <summary>
/// Service that parses and set rules for the robots.txt website.
/// </summary>
public class RobotsTxtService {
    private readonly IApiClient _apiClient;
    private readonly ILogger<RobotsTxtService> _logger;

    public const string RobotstxtUrl = "/robots.txt";
    public const string UserAgent = "User-agent:";
    public const string Disallow = "Disallow:";
    public const string Allow = "Allow:";
    public const string CrawlDelay = "Crawl-delay:";

    public RobotsTxtService(IApiClient apiClient, ILogger<RobotsTxtService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }
    
    private static bool TryGetDirectiveValue(string line, string directive, out string value)
    {
        if (line.StartsWith(directive, StringComparison.OrdinalIgnoreCase))
        {
            value = line.Substring(directive.Length).Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    public async Task<RobotsTxtResponse> GetRulesAsync(string rootUrl, CancellationToken cancellationToken = default) {
        try {
            var robotsUrl = new Uri(new Uri(rootUrl), RobotstxtUrl).ToString();
            
            var content = await _apiClient.GetStringAsync(robotsUrl, cancellationToken);
            var lines = content.Split('\n');

            var disallows = new List<string>();
            var allows = new List<string>();
            
            int delay = 0;
            bool applies = false;

            foreach (var raw in lines)
            {
                var line = raw.Trim();

                //Check if the directives apply to all crawlers 
                if (TryGetDirectiveValue(line, UserAgent, out var agent))
                {
                    applies = agent == "*";
                    continue;
                }

                if (!applies) continue;

                if (TryGetDirectiveValue(line, Disallow, out var path) && !string.IsNullOrEmpty(path))
                {
                    disallows.Add(path);
                }
                
                if (TryGetDirectiveValue(line, Allow, out var allow) && !string.IsNullOrEmpty(allow))
                {
                    allows.Add(allow);
                }

                if (TryGetDirectiveValue(line, CrawlDelay, out var delayStr) && int.TryParse(delayStr, out var d))
                {
                    delay = d * 1000;
                }
            }

            return new RobotsTxtResponse
            {
                Disallows = disallows,
                Allows = allows,
                Delay = delay,
            };
            
        } catch (Exception ex) {
            _logger.LogWarning("No robots.txt have been found or is malformed - " +
                               "No rules set for {root}", rootUrl);
            return new RobotsTxtResponse
            {
                Disallows = new(),
                Allows = new(),
                Delay = 0,
            };
        }
    }
    
    /// <summary>
    /// Method to check if a page is allowed or not. We need to consider both the case the page is explicitly allowed
    /// and if it is then return true. If not and is included in the disallowed list, return false. 
    /// </summary>
    /// <param name="pagePath"></param>
    /// <param name="disallows"></param>
    /// <returns></returns>
    public bool IsAllowed(string pagePath, List<string> disallows, List<string> allows)
    {
        var isExplicitlyAllowed = allows.Any(path => pagePath.StartsWith(path, StringComparison.OrdinalIgnoreCase));
        var isDisallowed = disallows.Any(path => pagePath.StartsWith(path, StringComparison.OrdinalIgnoreCase));

        if (isExplicitlyAllowed) return true;
        return !isDisallowed;
    }
}