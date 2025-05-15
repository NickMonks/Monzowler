using Monzowler.Domain.Entities;
using Monzowler.Persistence.Interfaces;

namespace Monzowler.Application.Results;

public class RepositoryResultHandler(ISiteMapRepository repository) : IResultHandler
{
    public Task HandleAsync(List<Page> pages)
    {
        return repository.SaveCrawlAsync(pages);
    }
}