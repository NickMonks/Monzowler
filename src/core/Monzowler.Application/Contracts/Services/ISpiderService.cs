using Monzowler.Domain.Entities;

namespace Monzowler.Application.Contracts.Services;

public interface ISpiderService
{
    Task<List<Page>> CrawlAsync(CrawlParameters  parameters);
}