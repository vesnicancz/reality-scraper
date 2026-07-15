using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Application.Interfaces.Repositories.Configuration;

public interface IScraperTaskRepository : IRepository<ScraperTask>
{
	Task<ScraperTask?> GetTaskWithDetailsAsync(Guid taskId, CancellationToken cancellationToken);
}