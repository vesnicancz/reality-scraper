using RealityScraper.Scraping;

namespace RealityScraper.Scheduler;

// Hosted service pro běh plánovače
public class SchedulerHostedService : BackgroundService
{
	private readonly List<Task> runningTasks = new List<Task>();
	private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

	private readonly ISchedulerRegistry schedulerRegistry;
	private readonly ILogger<SchedulerHostedService> logger;
	private readonly IServiceProvider serviceProvider;

	public SchedulerHostedService(
		ISchedulerRegistry schedulerRegistry,
		ILogger<SchedulerHostedService> logger,
		IServiceProvider serviceProvider)
	{
		this.schedulerRegistry = schedulerRegistry;
		this.logger = logger;
		this.serviceProvider = serviceProvider;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("Služba plánovače úloh byla spuštěna.");

		// Registrace úloh (v reálném použití by mohla být načtena z konfigurace)
		using (var scope = serviceProvider.CreateScope())
		{
			var registry = scope.ServiceProvider.GetRequiredService<ISchedulerRegistry>();
			registry.AddJob<ScraperServiceJob>("0 6,12,18 * * *", "Scraping realit");
		}

		while (!stoppingToken.IsCancellationRequested)
		{
			await CheckAndRunScheduledJobsAsync(stoppingToken);
			await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); // Kontrola každých 15 sekund
		}

		logger.LogInformation("Služba plánovače úloh byla zastavena.");
	}

	private async Task CheckAndRunScheduledJobsAsync(CancellationToken cancellationToken)
	{
		try
		{
			var scheduledJobs = schedulerRegistry.GetScheduledJobs();

			foreach (var job in scheduledJobs)
			{
				if (job.NextRun.HasValue && job.NextRun.Value <= DateTime.UtcNow)
				{
					await semaphore.WaitAsync(cancellationToken);
					try
					{
						// Spuštění úlohy v asynchronním režimu
						logger.LogInformation($"Spouštění úlohy: {job.Name}");
						var jobTask = Task.Run(async () =>
						{
							try
							{
								using var scope = serviceProvider.CreateScope();
								await job.Job.ExecuteAsync(cancellationToken);
								logger.LogInformation($"Úloha {job.Name} byla dokončena.");
							}
							catch (Exception ex)
							{
								logger.LogError(ex, $"Chyba při provádění úlohy {job.Name}: {ex.Message}");
							}
						}, cancellationToken);

						CleanupCompletedTasks();
						runningTasks.Add(jobTask);

						// Výpočet dalšího spuštění
						job.CalculateNextRun();
					}
					finally
					{
						semaphore.Release();
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, $"Chyba při kontrole naplánovaných úloh: {ex.Message}");
		}
	}

	private void CleanupCompletedTasks()
	{
		var completedTasks = runningTasks.Where(t => t.IsCompleted).ToList();
		foreach (var task in completedTasks)
		{
			runningTasks.Remove(task);
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		logger.LogInformation("Zastavování služby plánovače úloh...");

		// Počkání na dokončení všech běžících úloh
		await Task.WhenAll(runningTasks);

		await base.StopAsync(cancellationToken);
		logger.LogInformation("Služba plánovače úloh byla úspěšně zastavena.");
	}
}