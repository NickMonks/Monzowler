using Monzowler.Domain.Entities;

namespace Monzowler.Application.Contracts.Results;

/// <summary>
/// Because we have different sinks/results (e.g. either API or CLI)
/// we have this interface which chooses the prefered strategy
/// based on the application context. 
/// </summary>
public interface IResultHandler
{
    Task HandleAsync(List<Page> pages);
}