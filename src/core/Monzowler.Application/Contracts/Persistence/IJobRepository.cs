using Monzowler.Domain.Entities;

namespace Monzowler.Application.Contracts.Persistence;

public interface IJobRepository
{
    Task CreateAsync(Job job);
    Task UpdateStatusAsync(string jobId, JobStatus status, DateTime timestamp);
    Task MarkAsFailedAsync(string jobId, string errorMessage);
    Task<Job?> GetAsync(string jobId);
}