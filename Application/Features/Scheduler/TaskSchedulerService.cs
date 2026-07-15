using Microsoft.Extensions.Logging;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.Domain.Enums;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.Scheduler;

public class TaskSchedulerService : ITaskSchedulerService
{
	private readonly ITaskRepository taskRepository;
	private readonly IUnitOfWork unitOfWork;
	private readonly IScheduleTimeCalculator timeCalculator;
	private readonly IDateTimeProvider dateTimeProvider;
	private readonly ILogger<TaskSchedulerService> logger;

	public TaskSchedulerService(
		ITaskRepository taskRepository,
		IUnitOfWork unitOfWork,
		IScheduleTimeCalculator timeCalculator,
		IDateTimeProvider dateTimeProvider,
		ILogger<TaskSchedulerService> logger)
	{
		this.taskRepository = taskRepository;
		this.unitOfWork = unitOfWork;
		this.timeCalculator = timeCalculator;
		this.dateTimeProvider = dateTimeProvider;
		this.logger = logger;
	}

	public Task<DateTimeOffset?> CalculateNextRunTimeAsync(string cronExpression, DateTimeOffset fromTime, CancellationToken cancellationToken)
	{
		return Task.FromResult(timeCalculator.GetNextExecutionTime(cronExpression, fromTime));
	}

	public async Task<List<ScheduledTaskInfo>> LoadActiveTasksAsync(CancellationToken cancellationToken)
	{
		var activeTasks = await taskRepository.GetActiveTasksAsync(cancellationToken);
		var result = new List<ScheduledTaskInfo>();

		foreach (var dbTask in activeTasks)
		{
			if (!timeCalculator.IsValidExpression(dbTask.CronExpression))
			{
				logger.LogWarning("Úloha '{Name}' má neplatný cron výraz: '{CronExpression}'", dbTask.Name, dbTask.CronExpression);
				continue;
			}

			var nextRunTime = dbTask.NextRunAt ?? timeCalculator.GetNextExecutionTime(dbTask.CronExpression, dateTimeProvider.UtcNow);

			result.Add(new ScheduledTaskInfo
			{
				Id = dbTask.Id,
				Name = dbTask.Name,
				CronExpression = dbTask.CronExpression,
				TaskType = GetTaskType(dbTask),
				NextRunTime = nextRunTime,
				LastRunTime = dbTask.LastRunAt,
				IsRunning = false
			});

			logger.LogTrace("Načtena úloha '{Name}' s cron výrazem '{CronExpression}', další spuštění: {NextRunTime}", dbTask.Name, dbTask.CronExpression, nextRunTime);
		}

		return result;
	}

	public async Task UpdateTaskExecutionTimesAsync(Guid taskId, TaskExecutionResult result, CancellationToken cancellationToken)
	{
		await taskRepository.UpdateTaskExecutionResultAsync(taskId, result, cancellationToken);
		await unitOfWork.SaveChangesAsync(cancellationToken);
	}

	private static ScheduledTaskType GetTaskType(TaskBase task)
	{
		return task switch
		{
			ScraperTask => ScheduledTaskType.Scraper,
			RemovedListingsReportTask => ScheduledTaskType.RemovedListingsReport,
			_ => throw new NotSupportedException($"Neznámý typ úlohy: {task.GetType().Name}")
		};
	}
}