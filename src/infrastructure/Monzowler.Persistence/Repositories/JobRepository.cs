using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Monzowler.Application.Contracts.Persistence;
using Monzowler.Crawler.Models;
using Monzowler.Domain.Entities;
using Monzowler.Persistence.Models;

namespace Monzowler.Persistence.Repositories;

public class JobRepository(IAmazonDynamoDB dynamo) : IJobRepository
{
    private const string TableName = "Crawler-Jobs";

    public async Task CreateAsync(Job job)
    {
        var jobModel = JobDbModel.To(job);

        var item = new Dictionary<string, AttributeValue>
        {
            ["JobId"] = new AttributeValue { S = jobModel.JobId },
            ["Url"] = new AttributeValue { S = jobModel.Url },
            ["Status"] = new AttributeValue { S = jobModel.Status.ToString() },
        };

        if (jobModel.StartedAt.HasValue)
        {
            item["StartedAt"] = new AttributeValue { S = jobModel.StartedAt.Value.ToString("O") };
        }


        var request = new PutItemRequest
        {
            TableName = TableName,
            Item = item
        };

        await dynamo.PutItemAsync(request);
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

        await dynamo.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue> { ["JobId"] = new() { S = jobId } },
            AttributeUpdates = updates
        });
    }

    public async Task MarkAsFailedAsync(string jobId, string errorMessage)
    {
        await dynamo.UpdateItemAsync(new UpdateItemRequest
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
        var result = await dynamo.GetItemAsync(new GetItemRequest
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
