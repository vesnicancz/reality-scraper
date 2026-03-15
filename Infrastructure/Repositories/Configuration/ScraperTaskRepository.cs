using Microsoft.EntityFrameworkCore;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Features.Scheduler;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Infrastructure.Repositories.Configuration;

internal class ScraperTaskRepository : Repository<ScraperTask>, IScraperTaskRepository
{
	public ScraperTaskRepository(IDbContext dbContext)
		: base(dbContext)
	{
	}

	public async Task<List<ScraperTask>> GetActiveTasksAsync(CancellationToken cancellationToken)
	{
		return await dbContext.Set<ScraperTask>()
			.Include(t => t.Recipients)
			.Include(t => t.Targets)
			.Where(t => t.Enabled)
			.ToListAsync(cancellationToken);
	}

	public async Task<ScraperTask?> GetTaskWithDetailsAsync(Guid taskId, CancellationToken cancellationToken)
	{
		return await dbContext.Set<ScraperTask>()
			.Include(t => t.Recipients)
			.Include(t => t.Targets)
			.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
	}

	public async Task UpdateTaskExecutionResultAsync(Guid taskId, TaskExecutionResult result, CancellationToken cancellationToken)
	{
		var task = await dbContext.Set<ScraperTask>().FindAsync(new object[] { taskId }, cancellationToken);

		if (task != null)
		{
			task.SetLastRunAt(result.LastRunTime);
			task.SetNextRunAt(result.NextRunTime);
			task.SetLastRunLog(result.LastRunLog);
			task.SetLastRunSucceeded(result.Succeeded);
		}
	}
}