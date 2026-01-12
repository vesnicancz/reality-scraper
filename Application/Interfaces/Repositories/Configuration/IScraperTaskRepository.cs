using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Application.Interfaces.Repositories.Configuration;

public interface IScraperTaskRepository : IRepository<ScraperTask>
{
	Task<List<ScraperTask>> GetActiveTasksAsync(CancellationToken cancellationToken);

	Task<ScraperTask> GetTaskWithDetailsAsync(Guid taskId, CancellationToken cancellationToken);

	Task UpdateNextRunTimeAsync(Guid taskId, DateTimeOffset? nextRunTime, CancellationToken cancellationToken);

	Task UpdateLastRunTimeAsync(Guid taskId, DateTimeOffset lastRunTime, CancellationToken cancellationToken);
}