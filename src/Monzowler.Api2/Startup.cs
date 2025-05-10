using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Monzowler.Api;
using Monzowler.Crawler.Interfaces;
using Monzowler.Crawler.Parsers;
using Monzowler.Crawler.Repository;
using Monzowler.Crawler.Repository.Interfaces;
using Monzowler.Crawler.Service;
using Monzowler.HttpClient;

namespace Monzowler.Api2;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging();
        services.ConfigureAppSettings(Configuration);
        services.AddAwsServices(Configuration);
        services.AddApiClientServices(Configuration);

        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddSingleton<RobotsTxtService>();
        services.AddSingleton<BrowserProvider>();
        services.AddScoped<ISiteMapRepository, SiteMapRepository>();
        services.AddScoped<ISpiderService, SpiderService>();
        services.AddTransient<ISubParser, StaticHtmlParser>();
        services.AddTransient<ISubParser, RenderedHtmlParser>();
        services.AddTransient<IParser, ParserService>();
        services.AddSingleton<IJobRepository, JobRepository>();
        services.AddScoped<BackgroundCrawlService>();

        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Welcome to running ASP.NET Core on AWS Lambda");
            });
        });
    }
}