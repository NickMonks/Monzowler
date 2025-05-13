using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Moq;
using Monzowler.Crawler.Models;
using Monzowler.Persistence.Repositories;
namespace Monzowler.Unittest.Persistence;

public class JobRepositoryTests
{
    private readonly Mock<IAmazonDynamoDB> _dynamoMock;
    private readonly JobRepository _repository;

    public JobRepositoryTests()
    {
        _dynamoMock = new Mock<IAmazonDynamoDB>();
        _repository = new JobRepository(_dynamoMock.Object);
    }

    [Fact]
    public async Task CreateAsync_CallsPutItemWithCorrectData()
    {
        var job = new Job
        {
            JobId = "job-123",
            Url = "https://example.com",
            Status = JobStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        _dynamoMock.Setup(d =>
            d.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutItemResponse());

        await _repository.CreateAsync(job);

        _dynamoMock.Verify(d => d.PutItemAsync(It.Is<PutItemRequest>(r =>
            r.TableName == "Crawler-Jobs" &&
            r.Item["JobId"].S == job.JobId &&
            r.Item["Url"].S == job.Url &&
            r.Item["Status"].S == job.Status.ToString() &&
            r.Item.ContainsKey("StartedAt")
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesInProgressCorrectly()
    {
        var timestamp = DateTime.UtcNow;
        const string jobId = "job-456";

        _dynamoMock.Setup(d =>
            d.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateItemResponse());

        await _repository.UpdateStatusAsync(jobId, JobStatus.InProgress, timestamp);

        _dynamoMock.Verify(d => d.UpdateItemAsync(It.Is<UpdateItemRequest>(r =>
            r.Key["JobId"].S == jobId &&
            r.AttributeUpdates["Status"].Value.S == JobStatus.InProgress.ToString() &&
            r.AttributeUpdates.ContainsKey("StartedAt")
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesCompletedCorrectly()
    {
        var timestamp = DateTime.UtcNow;
        const string jobId = "job-789";

        _dynamoMock.Setup(d =>
            d.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateItemResponse());

        await _repository.UpdateStatusAsync(jobId, JobStatus.Completed, timestamp);

        _dynamoMock.Verify(d => d.UpdateItemAsync(It.Is<UpdateItemRequest>(r =>
            r.Key["JobId"].S == jobId &&
            r.AttributeUpdates["Status"].Value.S == JobStatus.Completed.ToString() &&
            r.AttributeUpdates.ContainsKey("CompletedAt")
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsFailedAsync_SetsErrorAndCompletedAt()
    {
        const string jobId = "job-999";
        const string error = "something went wrong";

        _dynamoMock.Setup(d =>
            d.UpdateItemAsync(It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateItemResponse());

        await _repository.MarkAsFailedAsync(jobId, error);

        _dynamoMock.Verify(d => d.UpdateItemAsync(It.Is<UpdateItemRequest>(r =>
            r.Key["JobId"].S == jobId &&
            r.AttributeUpdates["Status"].Value.S == JobStatus.Failed.ToString() &&
            r.AttributeUpdates["Error"].Value.S == error &&
            r.AttributeUpdates.ContainsKey("CompletedAt")
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ReturnsJob_WhenItemExists()
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["JobId"] = new AttributeValue { S = "job-abc" },
            ["Url"] = new AttributeValue { S = "https://example.com" },
            ["Status"] = new AttributeValue { S = "Completed" },
            ["StartedAt"] = new AttributeValue { S = DateTime.UtcNow.AddMinutes(-5).ToString("O") },
            ["CompletedAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
            ["Error"] = new AttributeValue { S = "none" }
        };

        _dynamoMock.Setup(d => d.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse { Item = item });

        var job = await _repository.GetAsync("job-abc");

        Assert.NotNull(job);
        Assert.Equal("job-abc", job.JobId);
        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.Equal("https://example.com", job.Url);
        Assert.Equal("none", job.Error);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenItemNotFound()
    {
        _dynamoMock.Setup(d => d.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse { IsItemSet = false });

        var job = await _repository.GetAsync("job-404");

        Assert.Null(job);
    }
}
