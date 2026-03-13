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
		logger.LogInformation("Scheduler service started at: {Time}", dateTimeProvider.GetCurrentTime());

		// Initial load from database
		await RefreshTasksFromDatabaseAsync(stoppingToken);

		while (!stoppingToken.IsCancellationRequested)
		{
			var timeToNextTask = CalculateTimeToNextTask();

			logger.LogTrace("Scheduler sleeping for {SleepTime}, next task due at {NextDue}",
				timeToNextTask,
				timeToNextTask < maxSleepInterval
					? dateTimeProvider.GetCurrentTime().Add(timeToNextTask)
					: (object)"no tasks scheduled");

			// Wait for either: a refresh signal (task changed) or timeout (next task is due)
			var wasSignaled = await refreshSignal.WaitForRefreshAsync(timeToNextTask, stoppingToken);

			if (wasSignaled)
			{
				logger.LogInformation("Scheduler woke up due to task change signal, refreshing from database");
				await RefreshTasksFromDatabaseAsync(stoppingToken);
			}
			else
			{
				// Timeout expired — execute due tasks, then refresh
				await CheckAndExecuteScheduledTasksAsync(stoppingToken);
			}
		}

		logger.LogInformation("Scheduler service terminated at: {Time}", dateTimeProvider.GetCurrentTime());
	}

	/// <summary>
	/// Calculates time until the next scheduled task, or maxSleepInterval if no tasks are pending.
	/// </summary>
	private TimeSpan CalculateTimeToNextTask()
	{
		var nextRunTime = scheduledTasks
			.Where(t => !t.IsRunning && t.NextRunTime.HasValue)
			.Select(t => t.NextRunTime)
			.DefaultIfEmpty()
			.Min();

		if (!nextRunTime.HasValue)
		{
			return maxSleepInterval;
		}

		var delay = nextRunTime.Value - dateTimeProvider.GetCurrentTime();

		// If task is already overdue, return zero
		return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
	}

	/// <summary>
	/// Updates the task list from the database.
	/// </summary>
	private async Task RefreshTasksFromDatabaseAsync(CancellationToken cancellationToken)
	{
		logger.LogTrace("Refreshing task list from database");

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
				logger.LogTrace("Task '{Name}' was removed from the list", taskToRemove.Name);
			}

			// Add or update tasks
			foreach (var dbTask in activeTasks)
			{
				var existingTask = scheduledTasks.FirstOrDefault(t => t.Id == dbTask.Id);
				if (existingTask == null)
				{
					scheduledTasks.Add(dbTask);
					logger.LogInformation("New task added '{Name}', next run: {NextRunTime}", dbTask.Name, dbTask.NextRunTime);
				}
				else if (!existingTask.IsRunning)
				{
					existingTask.Name = dbTask.Name;
					existingTask.CronExpression = dbTask.CronExpression;
					existingTask.ScrapingConfiguration = dbTask.ScrapingConfiguration;
					existingTask.NextRunTime = dbTask.NextRunTime;
					existingTask.LastRunTime = dbTask.LastRunTime;

					logger.LogTrace("Updated task '{Name}' from database, next run: {NextRunTime}", existingTask.Name, existingTask.NextRunTime);
				}
			}

			logger.LogInformation("Loaded and processed {Count} active tasks", activeTasks.Count);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error loading tasks from database");
			throw;
		}
	}

	/// <summary>
	/// Determines which tasks should be executed and runs them.
	/// </summary>
	private async Task CheckAndExecuteScheduledTasksAsync(CancellationToken cancellationToken)
	{
		var now = dateTimeProvider.GetCurrentTime();

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
			logger.LogInformation("Starting task '{Name}'", taskInfo.Name);

			using (var scope = serviceScopeFactory.CreateScope())
			{
				var task = (IScheduledTask)scope.ServiceProvider.GetRequiredService(typeof(ScraperServiceTask));

				await task.ExecuteAsync(taskInfo.ScrapingConfiguration, cancellationToken);

				var schedulerService = scope.ServiceProvider.GetRequiredService<ITaskSchedulerService>();

				var lastRunTime = dateTimeProvider.GetCurrentTime();
				var nextRunTime = await schedulerService.CalculateNextRunTimeAsync(taskInfo.CronExpression, lastRunTime, cancellationToken);

				await schedulerService.UpdateTaskExecutionTimesAsync(taskInfo.Id, lastRunTime, nextRunTime, cancellationToken);

				taskInfo.LastRunTime = lastRunTime;
				taskInfo.NextRunTime = nextRunTime;

				logger.LogInformation("Task '{Name}' completed successfully, next execution: {NextRunTime}", taskInfo.Name, nextRunTime);
			}
		}
		catch (OperationCanceledException)
		{
			logger.LogWarning("Task '{Name}' was cancelled", taskInfo.Name);
			throw;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error executing task '{Name}'", taskInfo.Name);

			try
			{
				using (var scope = serviceScopeFactory.CreateScope())
				{
					var schedulerService = scope.ServiceProvider.GetRequiredService<ITaskSchedulerService>();
					var nextRunTime = await schedulerService.CalculateNextRunTimeAsync(taskInfo.CronExpression, dateTimeProvider.GetCurrentTime(), cancellationToken);

					taskInfo.NextRunTime = nextRunTime;

					await schedulerService.UpdateTaskExecutionTimesAsync(taskInfo.Id, taskInfo.LastRunTime ?? dateTimeProvider.GetCurrentTime(), nextRunTime, cancellationToken);

					logger.LogWarning("Task '{Name}' will be run again at: {NextRunTime}", taskInfo.Name, nextRunTime);
				}
			}
			catch (Exception innerEx)
			{
				logger.LogError(innerEx, "Error calculating next run time for task '{Name}' after failure", taskInfo.Name);
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
		logger.LogInformation("Stopping Scheduler service");

		var runningTasks = scheduledTasks.Where(t => t.IsRunning).ToList();
		if (runningTasks.Count != 0)
		{
			logger.LogWarning("Waiting for {Count} running tasks to complete...", runningTasks.Count);

			var terminationDeadline = DateTime.Now.Add(terminationTimeout);

			do
			{
				await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
				runningTasks = scheduledTasks.Where(t => t.IsRunning).ToList();
			}
			while ((runningTasks.Count != 0) && (DateTime.Now < terminationDeadline) && !cancellationToken.IsCancellationRequested);

			if (runningTasks.Count != 0)
			{
				logger.LogError("Some tasks could not be completed in time. Terminating service.");
			}
			else
			{
				logger.LogInformation("All tasks completed successfully.");
			}
		}

		await base.StopAsync(cancellationToken);
	}
}