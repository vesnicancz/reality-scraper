using RealityScraper.Application.Features.Scheduler;

namespace RealityScraper.Application.Interfaces.Scheduler;

public interface ITaskSchedulerService
{
	Task<DateTimeOffset?> CalculateNextRunTimeAsync(string cronExpression, DateTimeOffset fromTime, CancellationToken cancellationToken);

	Task<List<ScheduledTaskInfo>> LoadActiveTasksAsync(CancellationToken cancellationToken);

	Task UpdateTaskExecutionTimesAsync(Guid taskId, TaskExecutionResult result, CancellationToken cancellationToken);
}