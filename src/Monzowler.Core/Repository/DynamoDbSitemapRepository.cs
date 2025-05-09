using Monzowler.Crawler.Interfaces;

namespace Monzowler.Crawler.Repository;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Collections.Concurrent;

public class DynamoDbSitemapRepository : ISitemapRepository {
    private readonly IAmazonDynamoDB _client;
    private const string TableName = "Sitemap";

    public DynamoDbSitemapRepository(IAmazonDynamoDB client) => _client = client;

    public async Task SaveSitemapAsync(string rootUrl, ConcurrentDictionary<string, List<string>> sitemap) {
        var domain = new Uri(rootUrl).Host;

        var writeRequests = sitemap.Select(kv => new WriteRequest {
            PutRequest = new PutRequest {
                Item = new Dictionary<string, AttributeValue> {
                    ["PK"] = new AttributeValue { S = domain },
                    ["SK"] = new AttributeValue { S = kv.Key },
                    ["Links"] = new AttributeValue {
                        L = kv.Value.Select(link => new AttributeValue { S = link }).ToList()
                    }
                }
            }
        }).ToList();

        foreach (var batch in writeRequests.Chunk(25)) {
            await _client.BatchWriteItemAsync(new BatchWriteItemRequest {
                RequestItems = new Dictionary<string, List<WriteRequest>> {
                    [TableName] = batch.ToList()
                }
            });
        }
    }

    public async Task<Dictionary<string, List<string>>> GetSitemapAsync(string rootUrl) {
        var domain = new Uri(rootUrl).Host;

        var query = new QueryRequest {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                [":pk"] = new AttributeValue { S = domain }
            }
        };

        var response = await _client.QueryAsync(query);

        return response.Items.ToDictionary(
            item => item["SK"].S,
            item => item["Links"].L.Select(l => l.S).ToList()
        );
    }
}