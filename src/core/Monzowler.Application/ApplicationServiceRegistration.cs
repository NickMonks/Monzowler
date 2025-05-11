using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monzowler.Application.Parsers;
using Monzowler.Application.Services;
using Monzowler.Application.Services.Parsers;
using Monzowler.Crawler.Interfaces;
using Monzowler.Crawler.Parsers;

namespace Monzowler.Application;

public static class ApplicationServiceRegistration
{
    public static void AddApplicationRegistration(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<RobotsTxtService>();
        services.AddSingleton<BrowserProvider>();
        services.AddScoped<ISpiderService, SpiderService>();
        services.AddTransient<ISubParser, StaticHtmlParser>();
        services.AddTransient<ISubParser, RenderedHtmlParser>();
        services.AddTransient<IParser, ParserService>();
    }
}