using Microsoft.EntityFrameworkCore;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Infrastructure.Repositories.Configuration;

internal class ReportTaskRepository : Repository<RemovedListingsReportTask>, IReportTaskRepository
{
	public ReportTaskRepository(IDbContext dbContext)
		: base(dbContext)
	{
	}

	public async Task<RemovedListingsReportTask?> GetTaskWithDetailsAsync(Guid taskId, CancellationToken cancellationToken)
	{
		return await dbContext.Set<RemovedListingsReportTask>()
			.Include(t => t.Recipients)
			.Include(t => t.Sources)
				.ThenInclude(s => s.ScraperTask)
			.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
	}
}
