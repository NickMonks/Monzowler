namespace Monzowler.Shared.Settings;

public class CrawlerSettings
{
    public required string UserAgent { get; init; }
    public int MaxConcurrency { get; init; }
    public int Timeout { get; init; }
}