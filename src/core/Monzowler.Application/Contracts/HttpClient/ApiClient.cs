namespace Monzowler.Application.Contracts.HttpClient;

public interface IApiClient
{
    Task<string> GetStringAsync(string url, CancellationToken ct);
}