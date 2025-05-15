using Monzowler.Crawler.Models;
using Monzowler.Domain.Responses;

namespace Monzowler.Application.Contracts.Services;

public interface IRobotsTxtService
{
    public Task<RobotsTxtResponse> GetRulesAsync(string rootUrl, string crawlerUserAgent = "*",
        CancellationToken cancellationToken = default);

    public bool IsAllowed(string pagePath, List<string> disallows, List<string> allows);
}