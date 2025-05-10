using Monzowler.Crawler.Models;
using Monzowler.Crawler.Repository.Interfaces;

namespace Monzowler.Crawler.Repository;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

public class JobRepository : IJobRepository
{
    private const string TableName = "Crawler-Jobs";
    private readonly IAmazonDynamoDB _dynamo;

    public JobRepository(IAmazonDynamoDB dynamo)
    {
        _dynamo = dynamo;
    }

    public async Task CreateAsync(Job job)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["JobId"] = new AttributeValue { S = job.JobId },
            ["Url"] = new AttributeValue { S = job.Url },
            ["Status"] = new AttributeValue { S = job.Status.ToString() },
        };

        if (job.StartedAt.HasValue)
        {
            item["StartedAt"] = new AttributeValue { S = job.StartedAt.Value.ToString("O") };
        }


        var request = new PutItemRequest
        {
            TableName = TableName,
            Item = item
        };

        await _dynamo.PutItemAsync(request);
    }

    public async Task UpdateStatusAsync(string jobId, JobStatus status, DateTime timestamp)
    {
        var updates = new Dictionary<string, AttributeValueUpdate>
        {
            ["Status"] = new(
                new AttributeValue { S = status.ToString() },
                AttributeAction.PUT
                )
        };

        if (status == JobStatus.InProgress)
        {
            updates["StartedAt"] = new(new AttributeValue { S = timestamp.ToString("O") }, AttributeAction.PUT);
        }
        else if (status == JobStatus.Completed)
        {
            updates["CompletedAt"] = new(new AttributeValue { S = timestamp.ToString("O") }, AttributeAction.PUT);
        }

        await _dynamo.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue> { ["JobId"] = new() { S = jobId } },
            AttributeUpdates = updates
        });
    }

    public async Task MarkAsFailedAsync(string jobId, string errorMessage)
    {
        await _dynamo.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue> { ["JobId"] = new() { S = jobId } },
            AttributeUpdates = new Dictionary<string, AttributeValueUpdate>
            {
                ["Status"] = new(new AttributeValue { S = nameof(JobStatus.Failed) }, AttributeAction.PUT),
                ["Error"] = new(new AttributeValue { S = errorMessage }, AttributeAction.PUT),
                ["CompletedAt"] = new(new AttributeValue { S = DateTime.UtcNow.ToString("O") }, AttributeAction.PUT)
            }
        });
    }

    public async Task<Job?> GetAsync(string jobId)
    {
        var result = await _dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue> { ["JobId"] = new() { S = jobId } }
        });

        if (!result.IsItemSet) return null;

        var item = result.Item;
        Enum.TryParse<JobStatus>(item["Status"].S, ignoreCase: true, out var status);

        return new Job
        {
            JobId = item["JobId"].S,
            Url = item["Url"].S,
            Status = status,
            StartedAt = item.TryGetValue("StartedAt", out var s) ? DateTime.Parse(s.S) : null,
            CompletedAt = item.TryGetValue("CompletedAt", out var c) ? DateTime.Parse(c.S) : null,
            Error = item.TryGetValue("Error", out var e) ? e.S : null
        };
    }
}
