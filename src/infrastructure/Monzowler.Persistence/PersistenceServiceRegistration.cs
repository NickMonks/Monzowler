using Amazon.DynamoDBv2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monzowler.Application.Contracts.Persistence;
using Monzowler.Persistence.Interfaces;
using Monzowler.Persistence.Repositories;

namespace Monzowler.Persistence;

public static class PersistenceServiceRegistration
{
    public static void AddPersistenceRegistration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDefaultAWSOptions(configuration.GetAWSOptions("AWS"));
        services.AddAWSService<IAmazonDynamoDB>();

        services.AddSingleton<IJobRepository, JobRepository>();
        services.AddScoped<ISiteMapRepository, SiteMapRepository>();
    }
}