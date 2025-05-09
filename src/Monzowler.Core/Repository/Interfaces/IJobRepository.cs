using Monzowler.Crawler.Models;

namespace Monzowler.Crawler.Repository.Interfaces;

public interface IJobRepository
{
    Task CreateAsync(Job job);
    Task UpdateStatusAsync(string jobId, JobStatus status, DateTime timestamp);
    Task MarkAsFailedAsync(string jobId, string errorMessage);
    Task<Job?> GetAsync(string jobId);
}