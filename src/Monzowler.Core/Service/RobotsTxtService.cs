using Microsoft.Extensions.Configuration;

namespace Monzowler.Crawler.Service;

using System.Net.Http;

public class RobotsTxtService {
    private readonly HttpClient _http = new();
    private readonly string _userAgent;

    public RobotsTxtService(IConfiguration config) {
        _userAgent = config["UserAgent"];
    }

    public async Task<(List<string> Disallows, int? CrawlDelay)> GetRulesAsync(string rootUrl) {
        try {
            var robotsUrl = new Uri(new Uri(rootUrl), "/robots.txt").ToString();
            
            //TODO: move request to specific httpclient project
            var content = await _http.GetStringAsync(robotsUrl);
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
            return (disallows, delay);
        } catch {
            return (new List<string>(), null);
        }
    }

    public bool IsAllowed(string pagePath, List<string> disallows) {
        return !disallows.Any(rule => pagePath.StartsWith(rule, StringComparison.OrdinalIgnoreCase));
    }
}