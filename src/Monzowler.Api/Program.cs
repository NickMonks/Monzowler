using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Monzowler.Crawler.Interfaces;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Parsers;
using Monzowler.Crawler.Repository;
using Monzowler.Crawler.Service;
using Monzowler.Crawler.Settings;
using Monzowler.HttpClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();

// Expose IConfiguration for DI
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddApiClientServices(builder.Configuration);

//TODO: Fix DynamoDB access error
var awsSettings = builder.Configuration
    .GetSection("AWS")
    .Get<AWSSettings>() ?? throw new NullReferenceException();

builder.Services.AddSingleton<IAmazonDynamoDB>(sp =>
{
    var config = new AmazonDynamoDBConfig
    {
        ServiceURL = awsSettings.ServiceURL,
        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsSettings.Region)
    };
    
    var credentials = new BasicAWSCredentials("test", "test");
    return new AmazonDynamoDBClient(credentials, config);
    
});

// DI registrations
builder.Services.AddSingleton<RobotsTxtService>();
builder.Services.AddScoped<ISitemapRepository, DynamoDbSitemapRepository>();
builder.Services.AddScoped<ICrawlerService, CrawlerService>();

//Parser registration
// builder.Services.AddTransient<IParser, HtmlParser>();
builder.Services.AddSingleton<BrowserProvider>();
builder.Services.AddTransient<ISubParser, HtmlParser>();
builder.Services.AddTransient<ISubParser, HeadlessParser>();
builder.Services.AddTransient<IParser, Parser>();

//Settings
builder.Services.Configure<CrawlerOptions>(
    builder.Configuration.GetSection("Crawler"));

//Add Http registration
var app = builder.Build();

//TODO: move this to a controller
app.MapPost("/crawl", async (CrawlRequest req, ICrawlerService crawler, ILogger<Program> logger) => {
    logger.LogInformation("Crawl endpoint hit with URL: {Url}", req.Url);
    var rootUrl = new Uri(req.Url).GetLeftPart(UriPartial.Authority).TrimEnd('/');
    var sitemap = await crawler.CrawlAsync(rootUrl);
    return Results.Ok(sitemap);
});

app.MapGet("/sitemap", async (string url, ISitemapRepository repo) => {
    var map = await repo.GetSitemapAsync(url);
    return map is null ? Results.NotFound() : Results.Ok(map);
});

//TODO: Add observability using jaeger 

app.Run();