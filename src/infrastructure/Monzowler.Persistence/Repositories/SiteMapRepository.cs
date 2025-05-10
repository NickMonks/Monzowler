using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Monzowler.Crawler.Models;
using Monzowler.Persistence.Interfaces;
using Monzowler.Persistence.Models;

namespace Monzowler.Persistence.Repositories;

public class SiteMapRepository(IAmazonDynamoDB dynamoDb) : ISiteMapRepository
{
    private const string TableName = "Crawler-Sitemap";

    public async Task SaveCrawlAsync(List<Page> pages)
    {
        var models = pages.Select(p => CrawlerDbModel.To(p)).ToList();
        const int batchSize = 20; // AWS allows up to 25 per batch

        foreach (var batch in models.Chunk(batchSize))
        {
            // Remove duplicates within the batch
            var deduplicatedBatch = batch
                .GroupBy(m => $"{m.Domain}::{m.PageUrl}")
                .Select(g => g.First())
                .ToList();

            var writeRequests = deduplicatedBatch.Select(model =>
            {
                var item = new Dictionary<string, AttributeValue>
                {
                    ["Domain"] = new AttributeValue { S = model.Domain },
                    ["Depth"] = new AttributeValue { N = model.Depth.ToString() },
                    ["PageUrl"] = new AttributeValue { S = model.PageUrl },
                    ["LastModified"] = new AttributeValue { S = model.LastModified }
                };

                // Optional attributes
                if (!string.IsNullOrWhiteSpace(model.Status))
                    item["Status"] = new AttributeValue { S = model.Status };

                if (!string.IsNullOrWhiteSpace(model.JobId))
                    item["JobId"] = new AttributeValue { S = model.JobId };

                // Only include Links if non-empty and valid
                if (model.Links is { Count: > 0 })
                {
                    var validLinks = model.Links
                        .Where(link => !string.IsNullOrWhiteSpace(link))
                        .Select(link => new AttributeValue { S = link })
                        .ToList();

                    if (validLinks.Count > 0)
                    {
                        item["Links"] = new AttributeValue { L = validLinks };
                    }
                }

                return new WriteRequest
                {
                    PutRequest = new PutRequest
                    {
                        Item = item
                    }
                };
            }).ToList();

            var request = new BatchWriteItemRequest
            {
                RequestItems = new Dictionary<string, List<WriteRequest>>
                {
                    [TableName] = writeRequests
                }
            };

            await dynamoDb.BatchWriteItemAsync(request);
        }
    }

    public async Task<List<Page>> GetCrawlsByDomainAsync(string domain)
    {
        var request = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "Domain = :v_domain",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                {":v_domain", new AttributeValue { S = domain }}
            }
        };

        var response = await dynamoDb.QueryAsync(request);

        var results = new List<CrawlerDbModel>();
        foreach (var item in response.Items)
        {
            var model = new CrawlerDbModel
            {
                Domain = item["Domain"].S,
                PageUrl = item["PageUrl"].S,
                Links = item.TryGetValue("Links", out var value2)
                    ? value2.L.Select(l => l.S).ToList()
                    : [],
                LastModified = item["LastModified"].S,
                Status = item.TryGetValue("Status", out var value) ? value.S : null,
                JobId = item.TryGetValue("JobId", out var value1) ? value1.S : null
            };

            results.Add(model);
        }

        return results.Select(r =>CrawlerDbModel.From(r)).ToList();
    }

    public async Task<List<Page>> GetCrawlsByJobIdAsync(string jobId)
    {
        var request = new QueryRequest
        {
            TableName = TableName,
            IndexName = "GSI_JobId",
            KeyConditionExpression = "JobId = :v_jobId",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                {":v_jobId", new AttributeValue { S = jobId }}
            }
        };

        var response = await dynamoDb.QueryAsync(request);

        return response.Items.Select(item =>
        {
            return new CrawlerDbModel
            {
                Domain = item["Domain"].S,
                PageUrl = item["PageUrl"].S,
                Depth = (item.TryGetValue("Depth", out var attr)
                         && int.TryParse(attr.N, out var depth))
                    ? depth
                    : 0,
                Links = item.TryGetValue("Links", out var value1)
                    ? value1.L.Select(l => l.S).ToList()
                    : [],
                LastModified = item["LastModified"].S,
                Status = item.TryGetValue("Status", out var value2) ? value2.S : null,
                JobId = item.TryGetValue("JobId", out var value3) ? value3.S : null
            };
        }).Select(r => CrawlerDbModel.From(r)).ToList();
    }
}