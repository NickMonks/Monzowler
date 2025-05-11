using System.Text.Json.Serialization;
using Monzowler.Api;
using Monzowler.Crawler.Settings;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();
builder.Services.Configure<CrawlerSettings>(builder.Configuration.GetSection("Crawler"));
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddStartupServices(builder.Configuration);
builder.Services.AddObservability(builder.Configuration);
builder.Services.AddScoped<BackgroundCrawler>();
builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();

//Needed for testing purposes
public partial class Program { }