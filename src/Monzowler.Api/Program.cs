using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Monzowler.Api;
using Monzowler.Crawler.Interfaces;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Parsers;
using Monzowler.Crawler.Repository;
using Monzowler.Crawler.Repository.Interfaces;
using Monzowler.Crawler.Service;
using Monzowler.HttpClient;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

var builder = WebApplication.CreateBuilder(args);
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

//Crawler Background Worker
builder.Services.AddScoped<BackgroundCrawlService>();

builder.Logging.SetMinimumLevel(LogLevel.Information);
var app = builder.Build();

app.MapPost("/crawl", (
    [FromBody] CrawlRequest req,
    [FromServices] BackgroundCrawlService crawler,
    [FromServices] ILogger<Program> logger) =>
{
    logger.LogInformation("Crawl requested for: {Url}", req.Url);
    try
    {
        var rootUrl = new Uri(req.Url).GetLeftPart(UriPartial.Authority).TrimEnd('/');
        var jobId = crawler.EnqueueCrawl(rootUrl);
        return Results.Accepted($"/crawl/{jobId}", new CrawlResponse { JobId = jobId, Status = JobStatus.Created });

    }
    catch (Exception e)
    {
        logger.LogError(e, "Failed to enqueue crawl job for {Url}", req.Url);

        return Results.Problem(
            title: "Failed to start crawl job",
            detail: e.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
});

app.MapGet("/crawl/{jobId}", async (
    [FromRoute] string jobId,
    [FromServices] IJobRepository repository,
    [FromServices] ILogger<Program> logger) =>
{
    try
    {
        var job = await repository.GetAsync(jobId);
        if (job is null)
        {
            return Results.NotFound(new { Message = $"Job '{jobId}' not found." });
        }

        return Results.Ok(job);
    }
    catch (Exception e)
    {
        logger.LogError(e, "Failed to fetch crawl job {JobId}", jobId);

        return Results.Problem(
            title: "Failed to retrieve crawl job",
            detail: e.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
});

app.MapGet("/sitemap/{jobId}", async (
    [FromRoute] string jobId,
    [FromServices] ISiteMapRepository repo) =>
{
    var map = await repo.GetCrawlsByJobIdAsync(jobId);
    return map is null ? Results.NotFound() : Results.Ok(map);
});

app.Run();