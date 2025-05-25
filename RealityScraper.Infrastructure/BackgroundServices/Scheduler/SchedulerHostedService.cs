using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scheduler;
using RealityScraper.Application.Features.Scraping;
using RealityScraper.Application.Interfaces.Scheduler;

namespace RealityScraper.Infrastructure.BackgroundServices.Scheduler;

/// <summary>
/// Hosted service for task management and scheduling
/// </summary>
public class SchedulerHostedService : BackgroundService
{
	private readonly IServiceScopeFactory serviceScopeFactory;
	private readonly ILogger<SchedulerHostedService> logger;

	private readonly List<ScheduledTaskInfo> scheduledTasks = new List<ScheduledTaskInfo>();
	private readonly TimeSpan taskCheckInterval = TimeSpan.FromSeconds(15);
	private readonly TimeSpan dbRefreshInterval = TimeSpan.FromMinutes(5);

	private DateTime lastDbCheckTime = DateTime.MinValue;

	private readonly TimeSpan terminationTimeout = TimeSpan.FromMinutes(5);

	public SchedulerHostedService(
		IServiceScopeFactory serviceScopeFactory,
		ILogger<SchedulerHostedService> logger)
	{
		this.serviceScopeFactory = serviceScopeFactory;
		this.logger = logger;
	}

	/// <summary>
	/// Main service method called at startup and running until service is stopped
	/// </summary>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("Scheduler service started at: {Time}", DateTimeOffset.Now);

		while (!stoppingToken.IsCancellationRequested)
		{
			// Periodic check of tasks in database
			if (DateTime.UtcNow >= lastDbCheckTime.Add(dbRefreshInterval))
			{
				await RefreshTasksFromDatabaseAsync(stoppingToken);
				lastDbCheckTime = DateTime.UtcNow;
			}

			// Check and execute tasks that need to be run
			await CheckAndExecuteScheduledTasksAsync(stoppingToken);

			// Pause before next check
			await Task.Delay(taskCheckInterval, stoppingToken);
		}

		logger.LogInformation("Scheduler service terminated at: {Time}", DateTimeOffset.Now);
	}

	/// <summary>
	/// Updates the task list from the database
	/// </summary>
	private async Task RefreshTasksFromDatabaseAsync(CancellationToken cancellationToken)
	{
		logger.LogTrace("Update the list of tasks from the database.");

		try
		{
			List<ScheduledTaskInfo> activeTasks;
			using (var scope = serviceScopeFactory.CreateScope())
			{
				var schedulerService = scope.ServiceProvider.GetRequiredService<ITaskSchedulerService>();

				// Load active tasks from database
				activeTasks = await schedulerService.LoadActiveTasksAsync(cancellationToken);
			}

			// Remove tasks that are no longer in database or no longer active
			var tasksToRemove = scheduledTasks
				.Where(t => !activeTasks.Any(a => a.Id == t.Id) || activeTasks.Any(a => (a.Id == t.Id) && !t.IsRunning))
				.ToList();

			foreach (var taskToRemove in tasksToRemove)
			{
				if (!taskToRemove.IsRunning)
				{
					scheduledTasks.Remove(taskToRemove);
					logger.LogTrace("Task '{Name}' was removed from the list", taskToRemove.Name);
				}
			}

			// Add or update tasks
			foreach (var dbTask in activeTasks)
			{
				var existingTask = scheduledTasks.FirstOrDefault(t => t.Id == dbTask.Id);
				if (existingTask == null)
				{
					// Add new task
					scheduledTasks.Add(dbTask);
					logger.LogInformation("New task added '{Name}', next run: {NextRunTime}", dbTask.Name, dbTask.NextRunTime);
				}
				else if (!existingTask.IsRunning)
				{
					// Update existing task that is not currently running
					existingTask.Name = dbTask.Name;
					existingTask.CronExpression = dbTask.CronExpression;
					existingTask.ScrapingConfiguration = dbTask.ScrapingConfiguration;

					// Update next run time only if not manually set or newer in database
					if (dbTask.NextRunTime.HasValue && (!existingTask.NextRunTime.HasValue || existingTask.NextRunTime.Value < dbTask.NextRunTime.Value))
					{
						existingTask.NextRunTime = dbTask.NextRunTime;
					}

					// Update last run time
					if (dbTask.LastRunTime.HasValue && (!existingTask.LastRunTime.HasValue || existingTask.LastRunTime.Value < dbTask.LastRunTime.Value))
					{
						existingTask.LastRunTime = dbTask.LastRunTime;
					}

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
	/// Determines which tasks should be executed and runs them
	/// </summary>
	private async Task CheckAndExecuteScheduledTasksAsync(CancellationToken cancellationToken)
	{
		// Execute tasks sequentially
		foreach (var taskInfo in scheduledTasks.Where(t => !t.IsRunning && t.NextRunTime.HasValue && t.NextRunTime.Value <= DateTime.UtcNow))
		{
			await ExecuteTaskAsync(taskInfo, cancellationToken);
		}
	}

	/// <summary>
	/// Executes one specific task
	/// </summary>
	private async Task ExecuteTaskAsync(ScheduledTaskInfo taskInfo, CancellationToken cancellationToken)
	{
		// Mark task as running
		taskInfo.IsRunning = true;

		try
		{
			logger.LogInformation("Starting task '{Name}'", taskInfo.Name);

			// Create new scope for each task
			using (var scope = serviceScopeFactory.CreateScope())
			{
				// Získání instance úlohy
				var task = (IScheduledTask)scope.ServiceProvider.GetRequiredService(typeof(ScraperServiceTask));

				// Get task instance
				await task.ExecuteAsync(taskInfo.ScrapingConfiguration, cancellationToken);

				var schedulerService = scope.ServiceProvider.GetRequiredService<ITaskSchedulerService>();

				// Save last run time and calculate next execution
				var lastRunTime = DateTime.UtcNow;
				var nextRunTime = await schedulerService.CalculateNextRunTimeAsync(taskInfo.CronExpression, lastRunTime, cancellationToken);

				// Update times in database
				await schedulerService.UpdateTaskExecutionTimesAsync(taskInfo.Id, lastRunTime, nextRunTime, cancellationToken);

				// Update task information
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

			// On error, try to recalculate next run time
			try
			{
				using (var scope = serviceScopeFactory.CreateScope())
				{
					var schedulerService = scope.ServiceProvider.GetRequiredService<ITaskSchedulerService>();
					var nextRunTime = await schedulerService.CalculateNextRunTimeAsync(taskInfo.CronExpression, DateTime.UtcNow, cancellationToken);

					taskInfo.NextRunTime = nextRunTime;

					// Update only next run time, not last run time
					await schedulerService.UpdateTaskExecutionTimesAsync(taskInfo.Id, taskInfo.LastRunTime ?? DateTime.UtcNow, nextRunTime, cancellationToken);

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
			// Mark task as completed
			taskInfo.IsRunning = false;
		}
	}

	/// <summary>
	/// Method called when service is stopping
	/// </summary>
	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		logger.LogInformation("Stopping Scheduler service");

		// Wait for running tasks to complete (optional with timeout)
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
				logger.LogError("Některé úlohy se nepodařilo dokončit včas. Ukončuji službu.");
			}
			else
			{
				logger.LogInformation("Všechny úlohy byly úspěšně dokončeny.");
			}
		}

		await base.StopAsync(cancellationToken);
	}
}