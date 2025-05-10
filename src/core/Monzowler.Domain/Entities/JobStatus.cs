using System.Text.Json.Serialization;

namespace Monzowler.Crawler.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobStatus
{
    Created,
    InProgress,
    Completed,
    Failed
}