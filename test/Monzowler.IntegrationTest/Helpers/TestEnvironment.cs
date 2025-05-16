using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using DotNet.Testcontainers.Builders;
using IContainer = DotNet.Testcontainers.Containers.IContainer;
namespace Monzowler.IntegrationTest.Helpers;

/// <summary>
/// Fixture class to set up and run our test containers in the integration test. We use to spin up
/// localstack testcontainer and run smoothly our tests
/// </summary>
public class TestEnvironment : IAsyncLifetime
{
    private readonly TestServer Server;
    public string BaseUrl => Server.BaseUrl;

    public string LocalstackEndpoint { get; set; }
    private IContainer LocalstackContainer { get; set; }

    public TestEnvironment()
    {
        Server = new TestServer();

        LocalstackContainer = new ContainerBuilder()
            .WithImage("localstack/localstack:latest")
            .WithName("localstack-" + Guid.NewGuid())
            .WithPortBinding(4566, true)
            .WithEnvironment("SERVICES", "dynamodb")
            .WithEnvironment("DEFAULT_REGION", "us-east-1")
            .WithEnvironment("HOSTNAME", "localhost")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(req =>
                    req.ForPort(4566).ForPath("/_localstack/health").ForStatusCode(HttpStatusCode.OK)))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await LocalstackContainer.StartAsync();
        var port = LocalstackContainer.GetMappedPublicPort(4566);
        LocalstackEndpoint = $"http://localhost:{port}";

        await CreateDynamoDbTablesAsync();
    }

    private async Task CreateDynamoDbTablesAsync()
    {
        var client = new AmazonDynamoDBClient(
            new BasicAWSCredentials("test", "test"),
            new AmazonDynamoDBConfig
            {
                ServiceURL = LocalstackEndpoint,
                AuthenticationRegion = "us-east-1",
                UseHttp = true,
            });

        // Create Crawler-Sitemap table with GSI
        var sitemapTable = new CreateTableRequest
        {
            TableName = "Crawler-Sitemap",
            KeySchema = new List<KeySchemaElement>
            {
                new("Domain", KeyType.HASH),
                new("PageUrl", KeyType.RANGE)
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new("Domain", ScalarAttributeType.S),
                new("PageUrl", ScalarAttributeType.S),
                new("JobId", ScalarAttributeType.S)
            },
            ProvisionedThroughput = new ProvisionedThroughput(10, 5),
            GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
            {
                new GlobalSecondaryIndex
                {
                    IndexName = "GSI_JobId",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new("JobId", KeyType.HASH)
                    },
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5)
                }
            }
        };

        await client.CreateTableAsync(sitemapTable);

        // Create Crawler-Jobs table
        var jobsTable = new CreateTableRequest
        {
            TableName = "Crawler-Jobs",
            KeySchema = new List<KeySchemaElement>
            {
                new("JobId", KeyType.HASH)
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new("JobId", ScalarAttributeType.S)
            },
            BillingMode = BillingMode.PAY_PER_REQUEST
        };

        await client.CreateTableAsync(jobsTable);
    }

    public async Task DisposeAsync()
    {
        await LocalstackContainer.DisposeAsync();
        Server.Dispose();
    }
}