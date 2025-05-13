using System.Text.Json.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Monzowler.Api;
using Monzowler.Crawler.Settings;
using Monzowler.Domain.Entities;
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

//Swagger OpenAPI Specification
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
    c.UseInlineDefinitionsForEnums();
    c.MapType<ParserStatusCode>(() => new OpenApiSchema
    {
        Type = "string",
        //So we can show strings on swagger instead of ugly int's
        Enum = Enum.GetNames(typeof(ParserStatusCode))
            .Select(name => new OpenApiString(name))
            .Cast<IOpenApiAny>()
            .ToList()
    });
});

System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();

//Needed for testing purposes
public partial class Program { }