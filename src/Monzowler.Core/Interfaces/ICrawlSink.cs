using Monzowler.Crawler.Models;

namespace Monzowler.Crawler.Sinks;

public interface ICrawlSink
{
    Task SaveAsync(List<Page> pages);
}