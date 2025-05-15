using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Monzowler.Crawler.Models;
using Monzowler.Domain.Entities;
using Monzowler.Persistence.Repositories;
using Moq;

namespace Monzowler.Unittest.Persistence;

public class SiteMapRepositoryTests
{
    private readonly Mock<IAmazonDynamoDB> _dynamoMock;
    private readonly SiteMapRepository _repository;

    public SiteMapRepositoryTests()
    {
        _dynamoMock = new Mock<IAmazonDynamoDB>();
        _repository = new SiteMapRepository(_dynamoMock.Object);
    }

    [Fact]
    public async Task SaveCrawlAsync_SavesPagesInBatches()
    {
        // Arrange
        var pages = Enumerable.Range(0, 10).Select(i => new Page
        {
            Domain = "example.com",
            PageUrl = $"https://example.com/page{i}",
            LastModified = DateTime.UtcNow.ToString("O"),
            Depth = i,
            Links = new List<string> { "https://link1.com", "https://link2.com" },
            Status = "OK",
            JobId = "job-123"
        }).ToList();

        _dynamoMock.Setup(d => d.BatchWriteItemAsync(
                It.IsAny<BatchWriteItemRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchWriteItemResponse());

        // Act
        await _repository.SaveCrawlAsync(pages);

        // Assert
        _dynamoMock.Verify(d => d.BatchWriteItemAsync(
            It.Is<BatchWriteItemRequest>(r =>
                r.RequestItems["Crawler-Sitemap"].Count == 10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCrawlsByDomainAsync_ReturnsPages()
    {
        // Arrange
        var items = new List<Dictionary<string, AttributeValue>>
        {
            new()
            {
                ["Domain"] = new AttributeValue { S = "example.com" },
                ["PageUrl"] = new AttributeValue { S = "https://example.com" },
                ["LastModified"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
                ["Links"] = new AttributeValue
                {
                    L = new List<AttributeValue>
                    {
                        new() { S = "https://link1.com" }
                    }
                },
                ["Status"] = new AttributeValue { S = "OK" },
                ["JobId"] = new AttributeValue { S = "job-123" }
            }
        };

        _dynamoMock.Setup(d => d.QueryAsync(It.IsAny<QueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResponse { Items = items });

        // Act
        var result = await _repository.GetCrawlsByDomainAsync("example.com");

        // Assert
        Assert.Single(result);
        Assert.Equal("example.com", result[0].Domain);
    }

    [Fact]
    public async Task GetCrawlsByJobIdAsync_ReturnsPages()
    {
        // Arrange
        var items = new List<Dictionary<string, AttributeValue>>
        {
            new()
            {
                ["Domain"] = new AttributeValue { S = "example.com" },
                ["PageUrl"] = new AttributeValue { S = "https://example.com/page1" },
                ["Depth"] = new AttributeValue { N = "2" },
                ["LastModified"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
                ["JobId"] = new AttributeValue { S = "job-123" },
                ["Links"] = new AttributeValue
                {
                    L = new List<AttributeValue>
                    {
                        new() { S = "https://link.com" }
                    }
                },
                ["Status"] = new AttributeValue { S = "OK" }
            }
        };

        _dynamoMock.Setup(d => d.QueryAsync(It.Is<QueryRequest>(r =>
                    r.IndexName == "GSI_JobId" && r.KeyConditionExpression.Contains("JobId")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResponse { Items = items });

        // Act
        var result = await _repository.GetCrawlsByJobIdAsync("job-123");

        // Assert
        Assert.Single(result);
        Assert.Equal("job-123", result[0].JobId);
        Assert.Equal(2, result[0].Depth);
    }
}