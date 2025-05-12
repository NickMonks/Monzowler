using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monzowler.Application.Contracts.Services;
using Monzowler.Application.Parsers;
using Monzowler.Application.Services;
using Monzowler.Crawler.Interfaces;
using Monzowler.Crawler.Parsers;

namespace Monzowler.Application;

public static class ApplicationServiceRegistration
{
    public static void AddApplicationRegistration(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IRobotsTxtService, RobotsTxtService>();
        services.AddSingleton<IBrowserProvider, BrowserProvider>();
        services.AddScoped<ISpiderService, SpiderService>();
        services.AddTransient<ISubParser, StaticHtmlParser>();
        services.AddTransient<ISubParser, RenderedHtmlParser>();
        services.AddTransient<IParser, ParserService>();
    }
}