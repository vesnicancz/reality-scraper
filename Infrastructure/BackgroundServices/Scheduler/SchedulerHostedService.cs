using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scheduler;
using RealityScraper.Application.Features.Scraping;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.SharedKernel;

namespace RealityScraper.Infrastructure.BackgroundServices.Scheduler;

/// <summary>
/// Event-driven hosted service for task management and scheduling.
/// Wakes up either when a domain event signals a change, or when the next task is due.
/// </summary>
public class SchedulerHostedService : BackgroundService
{
	private readonly IServiceScopeFactory serviceScopeFactory;
	private readonly ISchedulerRefreshSignal refreshSignal;
	private readonly IDateTimeProvider dateTimeProvider;
	private readonly ILogger<SchedulerHostedService> logger;

	private readonly List<ScheduledTaskInfo> scheduledTasks = new();
	private readonly TimeSpan maxSleepInterval = TimeSpan.FromHours(1);
	private readonly TimeSpan terminationTimeout = TimeSpan.FromMinutes(5);

	public SchedulerHostedService(
		IServiceScopeFactory serviceScopeFactory,
		ISchedulerRefreshSignal refreshSignal,
		IDateTimeProvider dateTimeProvider,
		ILogger<SchedulerHostedService> logger)
	{
		this.serviceScopeFactory = serviceScopeFactory;
		this.refreshSignal = refreshSignal;
		this.dateTimeProvider = dateTimeProvider;
		this.logger = logger;
	}

	/// <summary>
	/// Main service method — event-driven loop.
	/// </summary>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("Služba scheduleru spuštěna");

		// Initial load from database
		await RefreshTasksFromDatabaseAsync(stoppingToken);

		while (!stoppingToken.IsCancellationRequested)
		{
			var (timeToNextTask, nextTaskDueAt) = CalculateTimeToNextTask();

			logger.LogTrace("Scheduler spí po dobu {SleepTime}, další úloha: {NextDue}", timeToNextTask, nextTaskDueAt?.ToString() ?? "žádná");

			// Wait for either: a refresh signal (task changed) or timeout (next task is due)
			var wasSignaled = await refreshSignal.WaitForRefreshAsync(timeToNextTask, stoppingToken);

			if (wasSignaled)
			{
				logger.LogInformation("Přijat signál o změně úloh, obnovuji seznam z databáze");
				await RefreshTasksFromDatabaseAsync(stoppingToken);
			}
			else
			{
				// Timeout expired — execute due tasks
				await CheckAndExecuteScheduledTasksAsync(stoppingToken);
			}
		}

		logger.LogInformation("Služba scheduleru ukončena");
	}

	/// <summary>
	/// Calculates time until the next scheduled task, or maxSleepInterval if no tasks are pending.
	/// </summary>
	private (TimeSpan Delay, DateTimeOffset? NextDueAt) CalculateTimeToNextTask()
	{
		var nextRunTime = scheduledTasks
			.Where(t => !t.IsRunning && t.NextRunTime.HasValue)
			.Select(t => t.NextRunTime)
			.DefaultIfEmpty()
			.Min();

		if (!nextRunTime.HasValue)
		{
			return (maxSleepInterval, null);
		}

		var delay = nextRunTime.Value - dateTimeProvider.UtcNow;

		if (delay <= TimeSpan.Zero)
		{
			return (TimeSpan.Zero, nextRunTime);
		}

		return (delay < maxSleepInterval ? delay : maxSleepInterval, nextRunTime);
	}

	/// <summary>
	/// Updates the task list from the database.
	/// </summary>
	private async Task RefreshTasksFromDatabaseAsync(CancellationToken cancellationToken)
	{
		logger.LogTrace("Obnovuji seznam úloh z databáze");

		try
		{
			List<ScheduledTaskInfo> activeTasks;
			using (var scope = serviceScopeFactory.CreateScope())
			{
				var schedulerService = scope.ServiceProvider.GetRequiredService<ITaskSchedulerService>();
				activeTasks = await schedulerService.LoadActiveTasksAsync(cancellationToken);
			}

			// Remove tasks that are no longer in database or no longer active
			var tasksToRemove = scheduledTasks
				.Where(t => !t.IsRunning && !activeTasks.Any(a => a.Id == t.Id))
				.ToList();

			foreach (var taskToRemove in tasksToRemove)
			{
				scheduledTasks.Remove(taskToRemove);
				logger.LogTrace("Úloha '{Name}' odebrána ze seznamu", taskToRemove.Name);
			}

			// Add or update tasks
			foreach (var dbTask in activeTasks)
			{
				var existingTask = scheduledTasks.FirstOrDefault(t => t.Id == dbTask.Id);
				if (existingTask == null)
				{
					scheduledTasks.Add(dbTask);
					logger.LogInformation("Nová úloha '{Name}' přidána, další spuštění: {NextRunTime}", dbTask.Name, dbTask.NextRunTime);
				}
				else if (!existingTask.IsRunning)
				{
					existingTask.Name = dbTask.Name;
					existingTask.CronExpression = dbTask.CronExpression;
					existingTask.ScrapingConfiguration = dbTask.ScrapingConfiguration;
					existingTask.NextRunTime = dbTask.NextRunTime;
					existingTask.LastRunTime = dbTask.LastRunTime;

					logger.LogTrace("Úloha '{Name}' aktualizována, další spuštění: {NextRunTime}", existingTask.Name, existingTask.NextRunTime);
				}
			}

			logger.LogInformation("Načteno {Count} aktivních úloh", activeTasks.Count);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Chyba při načítání úloh z databáze");
			throw;
		}
	}

	/// <summary>
	/// Determines which tasks should be executed and runs them.
	/// </summary>
	private async Task CheckAndExecuteScheduledTasksAsync(CancellationToken cancellationToken)
	{
		var now = dateTimeProvider.UtcNow;

		foreach (var taskInfo in scheduledTasks.Where(t => !t.IsRunning && t.NextRunTime.HasValue && t.NextRunTime.Value <= now))
		{
			await ExecuteTaskAsync(taskInfo, cancellationToken);
		}
	}

	/// <summary>
	/// Executes one specific task.
	/// </summary>
	private async Task ExecuteTaskAsync(ScheduledTaskInfo taskInfo, CancellationToken cancellationToken)
	{
		taskInfo.IsRunning = true;

		try
		{
			logger.LogInformation("Spouštím úlohu '{Name}'", taskInfo.Name);

			using (var scope = serviceScopeFactory.CreateScope())
			{
				var task = (IScheduledTask)scope.ServiceProvider.GetRequiredService(typeof(ScraperServiceTask));

				await task.ExecuteAsync(taskInfo.ScrapingConfiguration, cancellationToken);

				var schedulerService = scope.ServiceProvider.GetRequiredService<ITaskSchedulerService>();

				var lastRunTime = dateTimeProvider.UtcNow;
				var nextRunTime = await schedulerService.CalculateNextRunTimeAsync(taskInfo.CronExpression, lastRunTime, cancellationToken);

				await schedulerService.UpdateTaskExecutionTimesAsync(taskInfo.Id, lastRunTime, nextRunTime, cancellationToken);

				taskInfo.LastRunTime = lastRunTime;
				taskInfo.NextRunTime = nextRunTime;

				logger.LogInformation("Úloha '{Name}' dokončena, další spuštění: {NextRunTime}", taskInfo.Name, nextRunTime);
			}
		}
		catch (OperationCanceledException)
		{
			logger.LogWarning("Úloha '{Name}' byla zrušena", taskInfo.Name);
			throw;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Chyba při provádění úlohy '{Name}'", taskInfo.Name);

			try
			{
				using (var scope = serviceScopeFactory.CreateScope())
				{
					var schedulerService = scope.ServiceProvider.GetRequiredService<ITaskSchedulerService>();
					var nextRunTime = await schedulerService.CalculateNextRunTimeAsync(taskInfo.CronExpression, dateTimeProvider.UtcNow, cancellationToken);

					taskInfo.NextRunTime = nextRunTime;

					await schedulerService.UpdateTaskExecutionTimesAsync(taskInfo.Id, taskInfo.LastRunTime ?? dateTimeProvider.UtcNow, nextRunTime, cancellationToken);

					logger.LogWarning("Úloha '{Name}' bude znovu spuštěna v: {NextRunTime}", taskInfo.Name, nextRunTime);
				}
			}
			catch (Exception innerEx)
			{
				logger.LogError(innerEx, "Chyba při výpočtu dalšího spuštění úlohy '{Name}'", taskInfo.Name);
			}
		}
		finally
		{
			taskInfo.IsRunning = false;
		}
	}

	/// <summary>
	/// Called when the service is stopping to ensure proper termination of running tasks.
	/// </summary>
	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		logger.LogInformation("Zastavuji službu scheduleru");

		var runningTasks = scheduledTasks.Where(t => t.IsRunning).ToList();
		if (runningTasks.Count != 0)
		{
			logger.LogWarning("Čekám na dokončení {Count} běžících úloh...", runningTasks.Count);

			var terminationDeadline = dateTimeProvider.UtcNow.Add(terminationTimeout);

			do
			{
				await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
				runningTasks = scheduledTasks.Where(t => t.IsRunning).ToList();
			}
			while ((runningTasks.Count != 0) && (dateTimeProvider.UtcNow < terminationDeadline) && !cancellationToken.IsCancellationRequested);

			if (runningTasks.Count != 0)
			{
				logger.LogError("Některé úlohy se nepodařilo dokončit včas. Ukončuji službu.");
			}
			else
			{
				logger.LogInformation("Všechny úlohy dokončeny.");
			}
		}

		await base.StopAsync(cancellationToken);
	}
}
