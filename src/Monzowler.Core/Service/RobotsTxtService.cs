using Monzowler.Crawler.Models;
using Monzowler.HttpClient.ApiClient;

namespace Monzowler.Crawler.Service;

public class RobotsTxtService {
    private readonly IApiClient _apiClient;

    public RobotsTxtService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<RobotsTxtResponse> GetRulesAsync(string rootUrl, CancellationToken cancellationToken = default) {
        try {
            var robotsUrl = new Uri(new Uri(rootUrl), "/robots.txt").ToString();
            
            var content = await _apiClient.GetStringAsync(robotsUrl, cancellationToken);
            var lines = content.Split('\n');

            var disallows = new List<string>();
            int? delay = null;
            bool applies = false;

            foreach (var raw in lines) {
                var line = raw.Trim();
                if (line.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase)) {
                    applies = line.Substring(11).Trim() == "*";
                }
                if (!applies) continue;

                if (line.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase)) {
                    var path = line.Substring(9).Trim();
                    if (!string.IsNullOrEmpty(path)) disallows.Add(path);
                }
                if (line.StartsWith("Crawl-delay:", StringComparison.OrdinalIgnoreCase)) {
                    if (int.TryParse(line.Substring(12).Trim(), out var d)) delay = d * 1000;
                }
            }

            return new RobotsTxtResponse
            {
                Disallows = disallows,
                Delay = delay,
            };
            
        } catch {
            //No disallows have been found, return empty list
            return new RobotsTxtResponse
            {
                Disallows = new List<string>(),
                Delay = 0,
            };
        }
    }
    
    /// <summary>
    /// Checks if a page path is disallowed or not by checking if any of the disallow url
    /// belongs to the pagePath argument. 
    /// </summary>
    /// <param name="pagePath"></param>
    /// <param name="disallows"></param>
    /// <returns></returns>
    public bool IsAllowed(string pagePath, List<string> disallows) {
        return !disallows.Any(rule => pagePath.StartsWith(rule, StringComparison.OrdinalIgnoreCase));
    }
}