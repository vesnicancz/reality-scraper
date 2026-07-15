using Microsoft.EntityFrameworkCore;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Features.Scheduler;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Infrastructure.Repositories.Configuration;

internal class TaskRepository : Repository<TaskBase>, ITaskRepository
{
	public TaskRepository(IDbContext dbContext)
		: base(dbContext)
	{
	}

	public async Task<List<TaskBase>> GetActiveTasksAsync(CancellationToken cancellationToken)
	{
		return await dbContext.Set<TaskBase>()
			.Where(t => t.Enabled)
			.ToListAsync(cancellationToken);
	}

	public async Task UpdateTaskExecutionResultAsync(Guid taskId, TaskExecutionResult result, CancellationToken cancellationToken)
	{
		var task = await dbContext.Set<TaskBase>().FindAsync(new object[] { taskId }, cancellationToken);

		if (task != null)
		{
			task.SetLastRunAt(result.LastRunTime);
			task.SetNextRunAt(result.NextRunTime);
			task.SetLastRunLog(result.LastRunLog);
			task.SetLastRunSucceeded(result.Succeeded);
		}
	}
}
