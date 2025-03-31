using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RealityScraper.Application.Features.Scheduling;
using RealityScraper.Application.Features.Scheduling.Configuration;
using RealityScraper.Application.Features.Scraping;

namespace RealityScraper.Infrastructure.BackgroundServices.Scheduler;

public class SchedulerHostedService : BackgroundService
{
	private readonly IServiceProvider serviceProvider;
	private readonly ILogger<SchedulerHostedService> logger;
	private readonly SchedulerSettings settings;

	private readonly List<ScheduledTaskInfo> scheduledTasks = new List<ScheduledTaskInfo>();
	private readonly TimeSpan taskRunTimeSpan = TimeSpan.FromSeconds(15);

	public SchedulerHostedService(
		IServiceProvider serviceProvider,
		IOptions<SchedulerSettings> options,
		ILogger<SchedulerHostedService> logger)
	{
		this.serviceProvider = serviceProvider;
		this.logger = logger;
		settings = options.Value;

		// Inicializace plánovaných úloh
		InitializeScheduledTasks();
	}

	private void InitializeScheduledTasks()
	{
		foreach (var taskConfig in settings.Tasks.Where(t => t.Enabled))
		{
			try
			{
				var cronExpression = CronExpression.Parse(taskConfig.CronExpression);

				scheduledTasks.Add(new ScheduledTaskInfo
				{
					Name = taskConfig.Name,
					CronExpression = cronExpression,
					ScrapingConfiguration = taskConfig.ScrapingConfiguration,
					NextRunTime = cronExpression.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Local)
				});

				logger.LogInformation("Task '{Name}' scheduled with cron expression '{CronExpression}'", taskConfig.Name, taskConfig.CronExpression);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to schedule task '{Name}' with cron expression '{CronExpression}'", taskConfig.Name, taskConfig.CronExpression);
			}
		}
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("Worker service started at: {Time}", DateTimeOffset.Now);

		while (!stoppingToken.IsCancellationRequested)
		{
			await CheckAndExecuteScheduledTasksAsync(stoppingToken);

			// Krátká pauza mezi kontrolami naplánovaných úloh
			await Task.Delay(taskRunTimeSpan, stoppingToken);
		}
	}

	private async Task CheckAndExecuteScheduledTasksAsync(CancellationToken stoppingToken)
	{
		foreach (var taskInfo in scheduledTasks.Where(t => !t.IsRunning && t.NextRunTime.HasValue && t.NextRunTime <= DateTime.UtcNow))
		{
			await ExecuteTaskAsync(taskInfo, stoppingToken);
		}
	}

	private async Task ExecuteTaskAsync(ScheduledTaskInfo taskInfo, CancellationToken stoppingToken)
	{
		// Označení úlohy jako běžící
		taskInfo.IsRunning = true;

		try
		{
			logger.LogInformation("Starting task '{Name}'", taskInfo.Name);

			// Získání instance úlohy
			var task = (IScheduledTask)serviceProvider.GetRequiredService(typeof(ScraperServiceTask));

			// Spuštění úlohy
			await task.ExecuteAsync(taskInfo.ScrapingConfiguration, stoppingToken);

			// Aktualizace času posledního spuštění
			taskInfo.LastRunTime = DateTime.UtcNow;

			logger.LogInformation("Task '{Name}' completed successfully", taskInfo.Name);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error executing task '{Name}'", taskInfo.Name);
		}
		finally
		{
			// Výpočet dalšího času spuštění
			taskInfo.NextRunTime = taskInfo.CronExpression.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Local);

			logger.LogInformation("Task '{Name}' next run time: {NextRunTime}", taskInfo.Name, taskInfo.NextRunTime);

			// Označení úlohy jako neběžící
			taskInfo.IsRunning = false;
		}
	}
}