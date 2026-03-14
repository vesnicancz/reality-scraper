using RealityScraper.Application.Features.Scheduler;
using RealityScraper.Application.Features.Scraping.Configuration;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Application.Interfaces.Scheduler;

public interface ITaskSchedulerService
{
	Task<DateTimeOffset?> CalculateNextRunTimeAsync(string cronExpression, DateTimeOffset fromTime, CancellationToken cancellationToken);

	Task<List<ScheduledTaskInfo>> LoadActiveTasksAsync(CancellationToken cancellationToken);

	Task UpdateTaskExecutionTimesAsync(Guid taskId, DateTimeOffset lastRunTime, DateTimeOffset? nextRunTime, CancellationToken cancellationToken);

	ScrapingConfiguration CreateScrapingConfigFromTask(ScraperTask task);
}