using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scraping;
using RealityScraper.Application.Features.Scraping.Configuration;
using RealityScraper.Application.Interfaces;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Scheduler;
using RealityScraper.Domain.Entities.Configuration;

namespace RealityScraper.Infrastructure.BackgroundServices.Scheduler;

public class SchedulerHostedService : BackgroundService
{
	private readonly IServiceScopeFactory serviceScopeFactory;
	private readonly ILogger<SchedulerHostedService> logger;

	private readonly List<ScheduledTaskInfo> scheduledTasks = new List<ScheduledTaskInfo>();
	private readonly TimeSpan taskRunTimeSpan = TimeSpan.FromSeconds(15);
	private readonly TimeSpan dbRefreshInterval = TimeSpan.FromMinutes(5); // Interval obnovení úloh z DB
	private DateTime lastDbCheckTime = DateTime.MinValue;

	public SchedulerHostedService(
		IServiceScopeFactory serviceScopeFactory,
		ILogger<SchedulerHostedService> logger)
	{
		this.serviceScopeFactory = serviceScopeFactory;
		this.logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("Scheduler service started at: {Time}", DateTimeOffset.Now);

		while (!stoppingToken.IsCancellationRequested)
		{
			// Pravidelná kontrola nových nebo upravených úloh v databázi
			if (DateTime.UtcNow >= lastDbCheckTime.Add(dbRefreshInterval))
			{
				await LoadTasksFromDatabaseAsync(stoppingToken);
				lastDbCheckTime = DateTime.UtcNow;
			}

			await CheckAndExecuteScheduledTasksAsync(stoppingToken);

			// Krátká pauza mezi kontrolami naplánovaných úloh
			await Task.Delay(taskRunTimeSpan, stoppingToken);
		}
	}

	private async Task LoadTasksFromDatabaseAsync(CancellationToken cancellationToken)
	{
		try
		{
			using var scope = serviceScopeFactory.CreateScope();
			var taskRepository = scope.ServiceProvider.GetRequiredService<IScraperTaskRepository>();
			var activeTasks = await taskRepository.GetActiveTasksAsync(cancellationToken);

			// Odstranění neaktivních úloh
			scheduledTasks.RemoveAll(t =>
				!activeTasks.Any(dbTask => dbTask.Id.ToString() == t.Id) ||
				activeTasks.Any(dbTask => dbTask.Id.ToString() == t.Id && dbTask.Enabled == false));

			// Aktualizace nebo přidání aktivních úloh
			foreach (var dbTask in activeTasks.Where(t => t.Enabled))
			{
				// Pokud není aktivní úloha spuštěna a není v seznamu, přidáme ji
				var existingTask = scheduledTasks.FirstOrDefault(t => t.Id == dbTask.Id.ToString());

				if (existingTask == null)
				{
					// Nová úloha
					try
					{
						var cronExpression = CronExpression.Parse(dbTask.CronExpression);
						var nextRunTime = dbTask.NextRunAt ?? cronExpression.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Local);

						// Vytvoření konfigurace scraperu
						var scrapingConfig = CreateScrapingConfigFromDbTask(dbTask);

						scheduledTasks.Add(new ScheduledTaskInfo
						{
							Id = dbTask.Id.ToString(),
							Name = dbTask.Name,
							CronExpression = cronExpression,
							ScrapingConfiguration = scrapingConfig,
							NextRunTime = nextRunTime,
							LastRunTime = dbTask.LastRunAt
						});

						logger.LogTrace("Loaded task '{Name}' from database with cron expression '{CronExpression}'", dbTask.Name, dbTask.CronExpression);
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Failed to load task '{Name}' with cron expression '{CronExpression}' from database", dbTask.Name, dbTask.CronExpression);
					}
				}
				else
				{
					// Existující úloha - aktualizujeme pouze pokud není zrovna spuštěná
					if (!existingTask.IsRunning)
					{
						try
						{
							// Aktualizace úlohy
							var cronExpression = CronExpression.Parse(dbTask.CronExpression);
							existingTask.Name = dbTask.Name;
							existingTask.CronExpression = cronExpression;
							existingTask.ScrapingConfiguration = CreateScrapingConfigFromDbTask(dbTask);

							// Aktualizace času příštího spuštění pouze pokud není již nastaven nebo pokud se změnil výraz
							if (!existingTask.NextRunTime.HasValue || dbTask.NextRunAt.HasValue)
							{
								existingTask.NextRunTime = dbTask.NextRunAt ?? cronExpression.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Local);
							}

							existingTask.LastRunTime = dbTask.LastRunAt;

							logger.LogTrace("Updated task '{Name}' from database", dbTask.Name);
						}
						catch (Exception ex)
						{
							logger.LogError(ex, "Failed to update task '{Name}' from database", dbTask.Name);
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error loading tasks from database");
		}
	}

	private static ScrapingConfiguration CreateScrapingConfigFromDbTask(ScraperTask dbTask)
	{
		var config = new ScrapingConfiguration
		{
			Id = dbTask.Id,
			EmailRecipients = dbTask.Recipients.Select(r => r.Email).ToList(),
			Scrapers = dbTask.Targets.Select(t => new ScraperConfiguration
			{
				ScraperType = t.ScraperType,
				Url = t.Url
			}).ToList()
		};

		return config;
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

			using (var scope = serviceScopeFactory.CreateScope())
			{
				// Získání instance úlohy
				var task = (IScheduledTask)scope.ServiceProvider.GetRequiredService(typeof(ScraperServiceTask));

				// Spuštění úlohy
				await task.ExecuteAsync(taskInfo.ScrapingConfiguration, stoppingToken);
			}

			// Uložení posledního času spuštění do databáze
			using (var scope = serviceScopeFactory.CreateScope())
			{
				var taskRepository = scope.ServiceProvider.GetRequiredService<IScraperTaskRepository>();
				var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

				var taskId = Guid.Parse(taskInfo.Id);
				await taskRepository.UpdateLastRunTimeAsync(taskId, DateTime.UtcNow, stoppingToken);
				await taskRepository.UpdateNextRunTimeAsync(taskId, taskInfo.NextRunTime, stoppingToken);

				await unitOfWork.SaveChangesAsync(stoppingToken);
			}

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

			logger.LogTrace("Task '{Name}' next run time: {NextRunTime}", taskInfo.Name, taskInfo.NextRunTime);

			// Označení úlohy jako neběžící
			taskInfo.IsRunning = false;
		}
	}
}