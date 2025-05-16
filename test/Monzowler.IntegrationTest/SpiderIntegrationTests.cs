using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monzowler.Api;
using Monzowler.Application.Contracts.HttpClient;
using Monzowler.Application.Contracts.Services;
using Monzowler.Domain.Entities;
using Monzowler.HttpClient;
using Monzowler.IntegrationTest.Helpers;
using Monzowler.Persistence;

namespace Monzowler.IntegrationTest;

public class SpiderIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IClassFixture<TestEnvironment>
{
    private readonly IServiceProvider _services;
    private readonly TestEnvironment _testEnvironment;

    public SpiderIntegrationTests(WebApplicationFactory<Program> factory, TestEnvironment testEnvironment)
    {
        _testEnvironment = testEnvironment;

        var customizedFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddJsonFile("appsettings.test.json", optional: false);
                //Override from the actual localstack endpoint - changes dynamically!
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["AWS:ServiceURL"] = testEnvironment.LocalstackEndpoint
                }!);
            });

            builder.ConfigureServices((context, services) =>
            {
                services.AddStartupServices(context.Configuration);
                services.AddPersistenceRegistration(context.Configuration);

                services.AddHttpClient<IApiClient, ApiClient>(client =>
                {
                    client.BaseAddress = new Uri(testEnvironment.BaseUrl);
                });
            });
        });

        _services = customizedFactory.Services;
    }

    [Fact]
    public async Task EnqueueCrawl_ShouldReturnCorrectPages_Static()
    {
        //Arrange
        using var scope = _services.CreateScope();
        var spider = scope.ServiceProvider.GetRequiredService<ISpiderService>();
        var seedUrl = _testEnvironment.BaseUrl + "/";
        var crawlParams = new CrawlParameters
        {
            RootUrl = seedUrl,
            JobId = Guid.NewGuid().ToString(),
            MaxDepth = 2,
            MaxRetries = 2
        };
        var expectedSitemap = Stubs.ExpectedStaticSiteMap(_testEnvironment.BaseUrl);
        
        

        //Act
        var actualSitemap = await spider.CrawlAsync(crawlParams);

        //Assert
        Assert.NotNull(actualSitemap);
        Assert.True(actualSitemap.Any(), "No URLs discovered");
        foreach (var expected in expectedSitemap)
        {
            actualSitemap.Should().ContainEquivalentOf(expected, options =>
                options
                    .Including(x => x.PageUrl)
                    .Including(x => x.Status)
                    .Including(x => x.Links));
        }
    }
    
    [Fact]
    public async Task EnqueueCrawl_ShouldReturnCorrectPages_Rendered()
    {
        //Arrange
        using var scope = _services.CreateScope();
        var spider = scope.ServiceProvider.GetRequiredService<ISpiderService>();
        var seedUrl = _testEnvironment.BaseUrl + "/rendered";
        var expectedSitemap = Stubs.ExpectedRenderedSiteMap(_testEnvironment.BaseUrl);
        var crawlParams = new CrawlParameters
        {
            RootUrl = seedUrl,
            JobId = Guid.NewGuid().ToString(),
            MaxDepth = 2,
            MaxRetries = 2
        };
        
        //Act
        var actualSitemap = await spider.CrawlAsync(crawlParams);

        //Assert
        Assert.NotNull(actualSitemap);
        Assert.True(actualSitemap.Any(), "No URLs discovered");
        foreach (var expected in expectedSitemap)
        {
            actualSitemap.Should().ContainEquivalentOf(expected, options =>
                options
                    .Including(x => x.PageUrl)
                    .Including(x => x.Status)
                    .Including(x => x.Links));
        }
    }
    
    [Fact]
    public async Task EnqueueCrawl_ShouldReturnCorrectPages_RobotsTxt()
    {
        //Arrange
        using var scope = _services.CreateScope();
        var spider = scope.ServiceProvider.GetRequiredService<ISpiderService>();
        var seedUrl = _testEnvironment.BaseUrl + "/with-robots-txt";
        var crawlParams = new CrawlParameters
        {
            RootUrl = seedUrl,
            JobId = Guid.NewGuid().ToString(),
            MaxDepth = 2,
            MaxRetries = 2
        };
        var expectedSitemap = Stubs.ExpectedSiteMapWithRobotsTxt(_testEnvironment.BaseUrl);

        //Act
        var actualSitemap = await spider.CrawlAsync(crawlParams);

        //Assert
        Assert.NotNull(actualSitemap);
        Assert.True(actualSitemap.Any(), "No URLs discovered");
        foreach (var expected in expectedSitemap)
        {
            actualSitemap.Should().ContainEquivalentOf(expected, options =>
                options
                    .Including(x => x.PageUrl)
                    .Including(x => x.Status)
                    .Including(x => x.Links));
        }
    }
}