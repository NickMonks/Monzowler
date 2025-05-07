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
        var item = new Dictionary<string, AttributeValue> {
            ["Url"] = new() { S = rootUrl },
            ["Pages"] = new() {
                M = sitemap.ToDictionary(
                    kv => kv.Key,
                    kv => new AttributeValue {
                        L = kv.Value.Select(u => new AttributeValue { S = u }).ToList()
                    }
                )
            }
        };
        await _client.PutItemAsync(new PutItemRequest {
            TableName = TableName,
            Item = item
        });
    }

    public async Task<Dictionary<string, List<string>>> GetSitemapAsync(string rootUrl) {
        var resp = await _client.GetItemAsync(new GetItemRequest {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue> {
                ["Url"] = new() { S = rootUrl }
            }
        });
        if (!resp.IsItemSet) return null;

        return resp.Item["Pages"].M.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.L.Select(av => av.S).ToList()
        );
    }
}