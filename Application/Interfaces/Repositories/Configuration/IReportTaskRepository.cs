using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Application.Interfaces.Repositories.Configuration;

public interface IReportTaskRepository : IRepository<RemovedListingsReportTask>
{
	Task<RemovedListingsReportTask?> GetTaskWithDetailsAsync(Guid taskId, CancellationToken cancellationToken);
}
