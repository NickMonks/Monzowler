using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monzowler.Application.Contracts.Services;
using Monzowler.Application.Parsers;
using Monzowler.Application.Results;
using Monzowler.Application.Services;
using Monzowler.Crawler.Parsers;

namespace Monzowler.Application;

public static class ApplicationServiceRegistration
{
    public static void AddApplicationRegistration(this IServiceCollection services)
    {
        services.AddSingleton<IRobotsTxtService, RobotsTxtService>();
        services.AddSingleton<IBrowserProvider, BrowserProvider>();
        services.AddSingleton<IPolitenessThrottlerService, PolitenessThrottlerService>();
        services.AddSingleton<IResultHandler, ConsoleResultHandler>();
        services.AddScoped<ISpiderService, SpiderService>();
        services.AddTransient<ISubParser, StaticHtmlParser>();
        services.AddTransient<ISubParser, RenderedHtmlParser>();
        services.AddTransient<IParser, ParserService>();
    }
}