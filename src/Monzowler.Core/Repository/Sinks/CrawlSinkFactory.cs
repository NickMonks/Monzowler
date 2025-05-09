using Microsoft.Extensions.DependencyInjection;

namespace Monzowler.Crawler.Sinks;

public class CrawlSinkFactory : ICrawlSinkFactory
{
    private readonly IServiceProvider _services;

    public CrawlSinkFactory(IServiceProvider services)
    {
        _services = services;
    }

    public ICrawlSink Create(CrawlSinkType type) => type switch
    {
        //TODO: complete this
        CrawlSinkType.DynamoDb => throw new NotImplementedException(),
        CrawlSinkType.Csv => throw new NotImplementedException(),
        CrawlSinkType.Json => throw new NotImplementedException(),
        _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported sink: {type}")
    };
}