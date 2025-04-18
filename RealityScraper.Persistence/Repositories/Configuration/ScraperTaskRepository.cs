using Microsoft.EntityFrameworkCore;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Domain.Entities.Configuration;
using RealityScraper.Persistence.Contexts;

namespace RealityScraper.Persistence.Repositories.Configuration;

public class ScraperTaskRepository : Repository<ScraperTask>, IScraperTaskRepository
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

	public async Task<ScraperTask> GetTaskWithDetailsAsync(Guid taskId, CancellationToken cancellationToken)
	{
		return await dbContext.Set<ScraperTask>()
			.Include(t => t.Recipients)
			.Include(t => t.Targets)
			.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
	}

	public async Task UpdateNextRunTimeAsync(Guid taskId, DateTime? nextRunTime, CancellationToken cancellationToken)
	{
		var task = await dbContext.Set<ScraperTask>().FindAsync(new object[] { taskId }, cancellationToken);

		if (task != null)
		{
			task.NextRunAt = nextRunTime;
			dbContext.Entry(task).State = EntityState.Modified;
		}
	}

	public async Task UpdateLastRunTimeAsync(Guid taskId, DateTime lastRunTime, CancellationToken cancellationToken)
	{
		var task = await dbContext.Set<ScraperTask>().FindAsync(new object[] { taskId }, cancellationToken);

		if (task != null)
		{
			task.LastRunAt = lastRunTime;
			dbContext.Entry(task).State = EntityState.Modified;
		}
	}
}