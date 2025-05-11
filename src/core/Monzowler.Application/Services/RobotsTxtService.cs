using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Monzowler.Crawler.Contracts.HttpClient;
using Monzowler.Crawler.Models;
using Monzowler.Shared.Observability;

namespace Monzowler.Application.Services;

/// <summary>
/// Services that parses and set rules for the robots.txt website.
/// </summary>
public class RobotsTxtService(IApiClient apiClient, ILogger<RobotsTxtService> logger)
{
    private const string RobotstxtUrl = "/robots.txt";
    private const string UserAgent = "User-agent:";
    private const string Disallow = "Disallow:";
    private const string Allow = "Allow:";
    private const string CrawlDelay = "Crawl-delay:";
    private const string LineComment = "#";
    private const string AllWildcard = "*";

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

    /// <summary>
    /// Gets the Robot txt rules from the domain crawled.
    /// As per specifications, when a matching group is found
    /// (where User-agent matches your crawler name or *),
    /// We apply only that group and ignore the rest. 
    /// </summary>
    /// <param name="rootUrl"></param>
    /// <param name="crawlerUserAgent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<RobotsTxtResponse> GetRulesAsync(string rootUrl, string crawlerUserAgent = "*", CancellationToken cancellationToken = default)
    {
        using var span = TracingHelper.Source.StartActivity("GetRulesRobot");
        span?.SetTag("rootUrl", rootUrl);
        span?.SetTag("userAgent", crawlerUserAgent);

        try
        {
            span?.AddEvent(new ActivityEvent("ParsingStarted"));

            var robotsUrl = new Uri(new Uri(rootUrl), RobotstxtUrl).ToString();
            var content = await apiClient.GetStringAsync(robotsUrl, cancellationToken);
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            span?.SetTag("lineCount", lines.Length);

            var disallows = new List<string>();
            var allows = new List<string>();
            int delay = 0;

            bool isRelevantGroup = false;
            var currentAgents = new List<string>();

            foreach (var raw in lines.Select(l => l.Trim()))
            {
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith(LineComment)) continue;

                if (TryGetDirectiveValue(raw, UserAgent, out var agent))
                {
                    agent = agent.Trim();
                    if (currentAgents.Count > 0)
                    {
                        isRelevantGroup = currentAgents.Contains(crawlerUserAgent, StringComparer.OrdinalIgnoreCase)
                                          || currentAgents.Contains(AllWildcard);
                        currentAgents.Clear();
                    }

                    currentAgents.Add(agent);
                    continue;
                }

                if (!isRelevantGroup && currentAgents.Count > 0)
                {
                    isRelevantGroup = currentAgents.Contains(crawlerUserAgent, StringComparer.OrdinalIgnoreCase)
                                      || currentAgents.Contains(AllWildcard);
                }

                if (!isRelevantGroup) continue;

                if (TryGetDirectiveValue(raw, Disallow, out var path) && !string.IsNullOrWhiteSpace(path))
                    disallows.Add(path);

                if (TryGetDirectiveValue(raw, Allow, out var allowPath) && !string.IsNullOrWhiteSpace(allowPath))
                    allows.Add(allowPath);

                if (TryGetDirectiveValue(raw, CrawlDelay, out var delayStr) && int.TryParse(delayStr, out var d))
                    delay = d * 1000;
            }

            span?.SetTag("disallowCount", disallows.Count);
            span?.SetTag("allowCount", allows.Count);
            span?.SetTag("crawlDelay", delay);
            span?.AddEvent(new ActivityEvent("ParsingCompleted"));

            return new RobotsTxtResponse
            {
                Disallows = disallows,
                Allows = allows,
                Delay = delay
            };
        }
        catch (Exception ex)
        {
            span?.SetStatus(ActivityStatusCode.Error, ex.Message);
            span?.AddEvent(new ActivityEvent("ParsingFailed"));

            logger.LogWarning(ex, "robots.txt could not be parsed for {root}", rootUrl);
            return new RobotsTxtResponse
            {
                Disallows = [],
                Allows = [],
                Delay = 0
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