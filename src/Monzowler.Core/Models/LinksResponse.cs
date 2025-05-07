namespace Monzowler.Crawler.Models;

public class LinkResult {
    public string Url { get; set; }
    public List<string> Links { get; set; } = new();
}