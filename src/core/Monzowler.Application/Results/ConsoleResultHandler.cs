using Monzowler.Application.Contracts.Results;
using Monzowler.Domain.Entities;

namespace Monzowler.Application.Results;

public class ConsoleResultHandler : IResultHandler
{
    public Task HandleAsync(List<Page> pages)
    {
        foreach (var page in pages)
        {
            Console.WriteLine($"----------------------------------------------------------------------------------------\n");
            Console.WriteLine($"[Depth: {page.Depth}] {page.PageUrl} - Status: {page.Status} - Links: [{string.Join(", ", page.Links)}]\n");
        }
        return Task.CompletedTask;
    }
}