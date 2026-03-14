using Microsoft.Extensions.Logging;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Features.Scraping.Configuration;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.Scheduler;

public class TaskSchedulerService : ITaskSchedulerService
{
	private readonly IScraperTaskRepository taskRepository;
	private readonly IUnitOfWork unitOfWork;
	private readonly IScheduleTimeCalculator timeCalculator;
	private readonly IDateTimeProvider dateTimeProvider;
	private readonly ILogger<TaskSchedulerService> logger;

	public TaskSchedulerService(
		IScraperTaskRepository taskRepository,
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

		foreach (var dbTask in activeTasks.Where(t => t.Enabled))
		{
			if (!timeCalculator.IsValidExpression(dbTask.CronExpression))
			{
				logger.LogWarning("Task '{Name}' has an invalid cron expression: '{CronExpression}'", dbTask.Name, dbTask.CronExpression);
				continue;
			}

			var nextRunTime = dbTask.NextRunAt ?? timeCalculator.GetNextExecutionTime(dbTask.CronExpression, dateTimeProvider.UtcNow);

			result.Add(new ScheduledTaskInfo
			{
				Id = dbTask.Id,
				Name = dbTask.Name,
				CronExpression = dbTask.CronExpression,
				ScrapingConfiguration = ScrapingConfigurationFactory.CreateFromTask(dbTask),
				NextRunTime = nextRunTime,
				LastRunTime = dbTask.LastRunAt,
				IsRunning = false
			});

			logger.LogTrace("Načtena úloha '{Name}' s cron výrazem '{CronExpression}', další spuštění: {NextRunTime}", dbTask.Name, dbTask.CronExpression, nextRunTime);
		}

		return result;
	}

	public async Task UpdateTaskExecutionTimesAsync(Guid taskId, DateTimeOffset lastRunTime, DateTimeOffset? nextRunTime, CancellationToken cancellationToken)
	{
		await taskRepository.UpdateLastRunTimeAsync(taskId, lastRunTime, cancellationToken);
		await taskRepository.UpdateNextRunTimeAsync(taskId, nextRunTime, cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);
	}
}