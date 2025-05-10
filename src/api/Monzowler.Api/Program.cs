using System.Text.Json.Serialization;
using Monzowler.Api;
using Monzowler.Application;
using Monzowler.Crawler.Settings;
using Monzowler.HttpClient;
using Monzowler.Persistence;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

var builder = WebApplication.CreateBuilder(args);

// Services and DI setup
builder.Services.AddLogging();

//Settings
builder.Services.Configure<CrawlerOptions>(builder.Configuration.GetSection("Crawler"));
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddPersistenceRegistration(builder.Configuration);
builder.Services.AddApplicationRegistration(builder.Configuration);
builder.Services.AddApiClientRegistration(builder.Configuration);

// DI registrations
builder.Services.AddScoped<BackgroundCrawlService>();

// Add controller support
builder.Services.AddControllers();

builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

// Use routing for controllers
app.MapControllers();

app.Run();