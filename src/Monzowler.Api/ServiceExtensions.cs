using Amazon.DynamoDBv2;
using Monzowler.Crawler.Settings;

namespace Monzowler.Api;

public static class ServiceExtensions
{
    public static void ConfigureAppSettings(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CrawlerOptions>(configuration.GetSection("Crawler"));
    }
    

    public static void AddAwsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDefaultAWSOptions(configuration.GetAWSOptions("AWS"));
        services.AddAWSService<IAmazonDynamoDB>();
    }
}