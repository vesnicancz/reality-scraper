using RealityScraper.Domain.Entities.Configuration;

namespace RealityScraper.Application.Interfaces.Repositories.Configuration;

public interface IScraperTaskRepository : IRepository<ScraperTask>
{
	Task<List<ScraperTask>> GetActiveTasksAsync(CancellationToken cancellationToken);

	Task<ScraperTask> GetTaskWithDetailsAsync(Guid taskId, CancellationToken cancellationToken);

	Task UpdateNextRunTimeAsync(Guid taskId, DateTime? nextRunTime, CancellationToken cancellationToken);

	Task UpdateLastRunTimeAsync(Guid taskId, DateTime lastRunTime, CancellationToken cancellationToken);
}