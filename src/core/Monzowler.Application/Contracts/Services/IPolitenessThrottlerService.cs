namespace Monzowler.Application.Contracts.Services;

public interface IPolitenessThrottlerService
{
    public Task EnforceAsync(string domain, CancellationToken ct = default);
    public void SetDelay(string domain, int delayMs);
}