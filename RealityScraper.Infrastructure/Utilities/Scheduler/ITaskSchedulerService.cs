using RealityScraper.Infrastructure.BackgroundServices.Scheduler;

namespace RealityScraper.Infrastructure.Utilities.Scheduler;

public interface ITaskSchedulerService
{
	Task<DateTime?> CalculateNextRunTimeAsync(string cronExpression, DateTime fromTime, CancellationToken cancellationToken);

	Task<List<ScheduledTaskInfo>> LoadActiveTasksAsync(CancellationToken cancellationToken);

	Task UpdateTaskExecutionTimesAsync(Guid taskId, DateTime lastRunTime, DateTime? nextRunTime, CancellationToken cancellationToken);
}