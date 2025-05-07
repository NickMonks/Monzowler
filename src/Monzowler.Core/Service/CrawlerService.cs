using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Monzowler.Crawler.Interfaces;
using Monzowler.Crawler.Service;

public class CrawlerService : ICrawlerService {
    private readonly HtmlParser _parser;
    private readonly RobotsTxtService _robots;
    private readonly ISitemapRepository _repo;
    private readonly int _maxConcurrency;
    private readonly int _defaultDelay;

    public CrawlerService(HtmlParser parser,
                          RobotsTxtService robots,
                          ISitemapRepository repo,
                          IConfiguration config) {
        _parser = parser;
        _robots = robots;
        _repo = repo;
        _maxConcurrency = config.GetValue<int>("Crawler:MaxConcurrency");
        _defaultDelay = config.GetValue<int>("Crawler:PolitenessDelayMilliseconds");
    }
    
    //TODO: create persistent queue and cache using Redis and SQS

    public async Task<Dictionary<string, List<string>>> CrawlAsync(string rootUrl, int maxDepth) {
        var sitemap = new ConcurrentDictionary<string, List<string>>();
        var queue = new ConcurrentQueue<(string Url, int Depth)>();
        queue.Enqueue((rootUrl, 0));
        
        //TODO: add the just added queue
        var visited = new ConcurrentDictionary<string, bool>();
        var semaphore = new SemaphoreSlim(_maxConcurrency);
        var tasks = new List<Task>();
        var baseUri = new Uri(rootUrl);
        var rootHost = baseUri.Host;

        // robots.txt
        var (disallows, crawlDelay) = await _robots.GetRulesAsync(rootUrl);
        var politeness = crawlDelay ?? _defaultDelay;
        
        //BFS approach
        while (queue.TryDequeue(out var item)) {
            if (item.Depth > maxDepth || visited.ContainsKey(item.Url)) continue;
            var path = new Uri(item.Url).AbsolutePath;
            if (!_robots.IsAllowed(path, disallows)) continue;
            if (!visited.TryAdd(item.Url, true)) continue;

            await semaphore.WaitAsync();
            tasks.Add(Task.Run(async () => {
                try {
                    //TODO: move this politeness to a specific client HTTP so only do the delay there, not here
                    await Task.Delay(politeness);
                    var links = await _parser.ParseLinksAsync(item.Url, rootHost);
                    sitemap[item.Url] = links;

                    foreach (var l in links) queue.Enqueue((l, item.Depth + 1));
                } catch {
                    // TODO: log error
                } finally {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        await _repo.SaveSitemapAsync(rootUrl, sitemap);
        return sitemap.ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}