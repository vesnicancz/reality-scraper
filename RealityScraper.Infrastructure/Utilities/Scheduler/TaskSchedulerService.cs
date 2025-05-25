using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scraping.Configuration;
using RealityScraper.Application.Interfaces;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Domain.Entities.Configuration;
using RealityScraper.Infrastructure.BackgroundServices.Scheduler;

namespace RealityScraper.Infrastructure.Utilities.Scheduler;

public class TaskSchedulerService : ITaskSchedulerService
{
	private readonly IScraperTaskRepository taskRepository;
	private readonly IUnitOfWork unitOfWork;
	private readonly IScheduleTimeCalculator timeCalculator;
	private readonly ILogger<TaskSchedulerService> logger;

	public TaskSchedulerService(
		IScraperTaskRepository taskRepository,
		IUnitOfWork unitOfWork,
		IScheduleTimeCalculator timeCalculator,
		ILogger<TaskSchedulerService> logger)
	{
		this.taskRepository = taskRepository;
		this.unitOfWork = unitOfWork;
		this.timeCalculator = timeCalculator;
		this.logger = logger;
	}

	public Task<DateTime?> CalculateNextRunTimeAsync(string cronExpression, DateTime fromTime, CancellationToken cancellationToken)
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

			var nextRunTime = dbTask.NextRunAt ?? timeCalculator.GetNextExecutionTime(dbTask.CronExpression, DateTime.UtcNow);

			result.Add(new ScheduledTaskInfo
			{
				Id = dbTask.Id,
				Name = dbTask.Name,
				CronExpression = dbTask.CronExpression,
				ScrapingConfiguration = CreateScrapingConfigFromTask(dbTask),
				NextRunTime = nextRunTime,
				LastRunTime = dbTask.LastRunAt,
				IsRunning = false
			});

			logger.LogTrace("Načtena úloha '{Name}' s cron výrazem '{CronExpression}', další spuštění: {NextRunTime}", dbTask.Name, dbTask.CronExpression, nextRunTime);
		}

		return result;
	}

	public async Task UpdateTaskExecutionTimesAsync(Guid taskId, DateTime lastRunTime, DateTime? nextRunTime, CancellationToken cancellationToken)
	{
		await taskRepository.UpdateLastRunTimeAsync(taskId, lastRunTime, cancellationToken);
		await taskRepository.UpdateNextRunTimeAsync(taskId, nextRunTime, cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);
	}

	/// <summary>
	/// Vytvoří konfiguraci scraperu z entity úlohySS
	/// </summary>
	private ScrapingConfiguration CreateScrapingConfigFromTask(ScraperTask dbTask)
	{
		return new ScrapingConfiguration
		{
			Id = dbTask.Id,
			EmailRecipients = dbTask.Recipients?.Select(r => r.Email).ToList() ?? new List<string>(),
			Scrapers = dbTask.Targets?.Select(t => new ScraperConfiguration
			{
				ScraperType = t.ScraperType,
				Url = t.Url
			}).ToList() ?? new List<ScraperConfiguration>()
		};
	}
}