using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Features.Scheduler;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Application.Interfaces.Repositories.Configuration;

public interface IScraperTaskRepository : IRepository<ScraperTask>
{
	Task<List<ScraperTask>> GetActiveTasksAsync(CancellationToken cancellationToken);

	Task<ScraperTask?> GetTaskWithDetailsAsync(Guid taskId, CancellationToken cancellationToken);

	Task UpdateTaskExecutionResultAsync(Guid taskId, TaskExecutionResult result, CancellationToken cancellationToken);
}