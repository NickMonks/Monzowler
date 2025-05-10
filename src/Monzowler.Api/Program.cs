using System.Text.Json.Serialization;
using Monzowler.Api;
using Monzowler.Crawler.Interfaces;
using Monzowler.Crawler.Parsers;
using Monzowler.Crawler.Repository;
using Monzowler.Crawler.Repository.Interfaces;
using Monzowler.Crawler.Service;
using Monzowler.HttpClient;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

var builder = WebApplication.CreateBuilder(args);

// Services and DI setup
builder.Services.AddLogging();
builder.Services.ConfigureAppSettings(builder.Configuration);
builder.Services.AddAwsServices(builder.Configuration);
builder.Services.AddApiClientServices(builder.Configuration);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// DI registrations
builder.Services.AddSingleton<RobotsTxtService>();
builder.Services.AddSingleton<BrowserProvider>();
builder.Services.AddScoped<ISiteMapRepository, SiteMapRepository>();
builder.Services.AddScoped<ISpiderService, SpiderService>();
builder.Services.AddTransient<ISubParser, StaticHtmlParser>();
builder.Services.AddTransient<ISubParser, RenderedHtmlParser>();
builder.Services.AddTransient<IParser, ParserService>();
builder.Services.AddSingleton<IJobRepository, JobRepository>();
builder.Services.AddScoped<BackgroundCrawlService>();

// Add controller support
builder.Services.AddControllers();

builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

// Use routing for controllers
app.MapControllers();

app.Run();