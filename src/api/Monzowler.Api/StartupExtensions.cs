using Monzowler.Application;
using Monzowler.Crawler.Settings;
using Monzowler.HttpClient;
using Monzowler.Persistence;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Monzowler.Api;

public static class StartupExtensions
{
    public static void AddStartupServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistenceRegistration(configuration);
        services.AddApplicationRegistration(configuration);
        services.AddApiClientRegistration(configuration);
    }
    public static void AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var observabilitySettings = configuration
            .GetSection(nameof(ObservabilitySettings))
            .Get<ObservabilitySettings>() ?? throw new NullReferenceException();

        services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddSource(observabilitySettings.ServiceName)
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(
                            serviceName: observabilitySettings.ServiceName,
                            serviceVersion: observabilitySettings.ServiceVersion
                        ))
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter(opts =>
                {
                    opts.Endpoint = new Uri(observabilitySettings.JaegerExporterUri);
                })
                .AddConsoleExporter();
        });
    }
}