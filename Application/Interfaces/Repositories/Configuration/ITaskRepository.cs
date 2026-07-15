using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Features.Scheduler;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Application.Interfaces.Repositories.Configuration;

public interface ITaskRepository : IRepository<TaskBase>
{
	Task<List<TaskBase>> GetActiveTasksAsync(CancellationToken cancellationToken);

	Task UpdateTaskExecutionResultAsync(Guid taskId, TaskExecutionResult result, CancellationToken cancellationToken);
}
