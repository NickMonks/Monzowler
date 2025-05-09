namespace Monzowler.Crawler.Sinks;

public interface ICrawlSinkFactory
{
    ICrawlSink Create(CrawlSinkType type);
}