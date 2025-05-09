using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.AspNetCore.Mvc;
using Monzowler.Api;
using Monzowler.Crawler.Interfaces;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Parsers;
using Monzowler.Crawler.Repository;
using Monzowler.Crawler.Service;
using Monzowler.Crawler.Settings;
using Monzowler.HttpClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();
builder.Services.ConfigureAppSettings(builder.Configuration);
builder.Services.AddAwsServices(builder.Configuration);
builder.Services.AddApiClientServices(builder.Configuration);

// DI registrations
builder.Services.AddSingleton<RobotsTxtService>();
builder.Services.AddSingleton<BrowserProvider>();
builder.Services.AddScoped<ISiteMapRepository, SiteMapRepository>();
builder.Services.AddScoped<ISpiderService, SpiderService>();
builder.Services.AddTransient<ISubParser, StaticHtmlParser>();
builder.Services.AddTransient<ISubParser, RenderedHtmlParser>();
builder.Services.AddTransient<IParser, ParserService>();

//Add Http registration
var app = builder.Build();

app.MapPost("/crawl", async (
    [FromBody] CrawlRequest req, 
    [FromServices] ISpiderService spider, 
    [FromServices] ILogger<Program> logger) => {
    
    logger.LogInformation("Crawl endpoint hit with URL: {Url}", req.Url);
    var rootUrl = new Uri(req.Url).GetLeftPart(UriPartial.Authority).TrimEnd('/');
    var sitemap = await spider.CrawlAsync(rootUrl);
    return Results.Ok(sitemap);
});

app.MapGet("/sitemap", async (
    [FromBody] SiteMapRequest req, 
    [FromServices] ISiteMapRepository repo) => {
    var map = await repo.GetCrawlsByDomainAsync(req.Url);
    return map is null ? Results.NotFound() : Results.Ok(map);
});

//TODO: Add observability using jaeger 

app.Run();