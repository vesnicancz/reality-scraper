using Cronos;

namespace RealityScraper.Scheduler;

// Implementace registru úloh
public class SchedulerRegistry : ISchedulerRegistry
{
	private readonly List<ScheduledJob> scheduledJobs = new List<ScheduledJob>();
	private readonly IJobFactory jobFactory;
	private readonly ILogger<SchedulerRegistry> logger;

	public SchedulerRegistry(IJobFactory jobFactory, ILogger<SchedulerRegistry> logger)
	{
		this.jobFactory = jobFactory;
		this.logger = logger;
	}

	public void AddJob<T>(string cronExpression, string jobName = null)
		where T : IJob
	{
		var job = jobFactory.Create<T>();
		AddJob(job, cronExpression, jobName);
	}

	public void AddJob(IJob job, string cronExpression, string jobName = null)
	{
		ArgumentNullException.ThrowIfNull(job);
		if (string.IsNullOrWhiteSpace(cronExpression))
		{
			throw new ArgumentNullException(nameof(cronExpression));
		}

		try
		{
			// Použití balíčku Cronos pro validaci a parsování cron výrazu
			var cronSchedule = CronExpression.Parse(cronExpression);
			var name = jobName ?? job.GetType().Name;

			scheduledJobs.Add(new ScheduledJob(job, cronSchedule, name));
			logger.LogInformation($"Úloha {name} byla úspěšně naplánována s cron výrazem: {cronExpression}");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, $"Chyba při přidávání úlohy {jobName ?? job.GetType().Name}: {ex.Message}");
			throw;
		}
	}

	public IReadOnlyCollection<ScheduledJob> GetScheduledJobs()
		=> scheduledJobs.AsReadOnly();
}