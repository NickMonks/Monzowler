namespace Monzowler.Shared.Settings;

public class ObservabilitySettings
{
    public required string JaegerExporterUri { get; init; }
    public required string ServiceName { get; init; }
    public required string ServiceVersion { get; init; }
}