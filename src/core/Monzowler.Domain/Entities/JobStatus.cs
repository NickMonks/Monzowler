using System.Text.Json.Serialization;

namespace Monzowler.Domain.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobStatus
{
    Created,
    InProgress,
    Completed,
    Failed
}