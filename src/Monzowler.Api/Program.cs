using Amazon.DynamoDBv2;
using Monzowler.Crawler.Interfaces;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Repository;
using Monzowler.Crawler.Service;

var builder = WebApplication.CreateBuilder(args);

// Expose IConfiguration for DI
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

//TODO: Fix DynamoDB access error
var serviceUrl = builder.Configuration["AWS:ServiceURL"];
var region = builder.Configuration["AWS:Region"];

builder.Services.AddSingleton<IAmazonDynamoDB>(sp => new AmazonDynamoDBClient(new AmazonDynamoDBConfig {
    ServiceURL = serviceUrl,
    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
}));

// DI registrations
builder.Services.AddSingleton<HtmlParser>();
builder.Services.AddSingleton<RobotsTxtService>();
builder.Services.AddScoped<ISitemapRepository, DynamoDbSitemapRepository>();
builder.Services.AddScoped<ICrawlerService, CrawlerService>();

var app = builder.Build();

//TODO: move this to a controller
app.MapPost("/crawl", async (CrawlRequest req, ICrawlerService crawler) => {
    var rootUrl = new Uri(req.Url).GetLeftPart(UriPartial.Authority).TrimEnd('/');
    var sitemap = await crawler.CrawlAsync(rootUrl, req.MaxDepth);
    return Results.Ok(sitemap);
});

app.MapGet("/sitemap", async (string url, ISitemapRepository repo) => {
    var map = await repo.GetSitemapAsync(url);
    return map is null ? Results.NotFound() : Results.Ok(map);
});

app.Run();