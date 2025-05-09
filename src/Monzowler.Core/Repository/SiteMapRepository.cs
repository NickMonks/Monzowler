using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Monzowler.Crawler.Interfaces;
using Monzowler.Crawler.Models;
using Monzowler.Crawler.Repository.Models;

public class SiteMapRepository : ISiteMapRepository
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private const string TableName = "Crawler-Sitemap";

    public SiteMapRepository(IAmazonDynamoDB dynamoDb)
    {
        _dynamoDb = dynamoDb;
    }

    public async Task SaveCrawlAsync(List<Page> pages)
    {
        var models = pages.Select(p => CrawlerDbModel.To(p)).ToList();
        const int batchSize = 20; //I believe AWS limit this to be 25 concurrent putitems

        foreach (var batch in models.Chunk(batchSize))
        {
            // DynamoDB doesn't allow duplicates - ensure we are not setting duplicates in one batch request
            var deduplicatedBatch = batch
                .GroupBy(m => $"{m.Domain}::{m.PageUrl}")
                .Select(g => g.First())
                .ToList();
            
            var writeRequests = deduplicatedBatch.Select(model =>
            {
                var item = new Dictionary<string, AttributeValue>
                {
                    ["Domain"] = new() { S = model.Domain },
                    ["PageUrl"] = new() { S = model.PageUrl },
                    ["Links"] = new()
                    {
                        L = model.Links.Select(link => new AttributeValue { S = link }).ToList()
                    },
                    ["LastModified"] = new() { S = model.LastModified }
                };

                if (!string.IsNullOrEmpty(model.Status))
                    item["Error"] = new AttributeValue { S = model.Status };

                if (!string.IsNullOrEmpty(model.JobId))
                    item["JobId"] = new AttributeValue { S = model.JobId };
                
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

            await _dynamoDb.BatchWriteItemAsync(request);
        }
    }

    public async Task<List<CrawlerDbModel>> GetCrawlsByDomainAsync(string domain)
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

        var response = await _dynamoDb.QueryAsync(request);

        var results = new List<CrawlerDbModel>();
        foreach (var item in response.Items)
        {
            var model = new CrawlerDbModel
            {
                Domain = item["Domain"].S,
                PageUrl = item["PageUrl"].S,
                Links = item.ContainsKey("Links")
                    ? item["Links"].L.Select(l => l.S).ToList()
                    : new List<string>(),
                LastModified = item["LastModified"].S,
                Status = item.ContainsKey("Error") ? item["Error"].S : null,
                JobId = item.ContainsKey("JobId") ? item["JobId"].S : null
            };

            results.Add(model);
        }

        return results;
    }
}