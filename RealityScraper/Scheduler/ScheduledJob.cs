using Cronos;

namespace RealityScraper.Scheduler;

// Model reprezentující naplánovanou úlohu
public class ScheduledJob
{
	public IJob Job { get; }

	public CronExpression CronSchedule { get; }

	public string Name { get; }

	public DateTime? NextRun { get; private set; }

	public ScheduledJob(IJob job, CronExpression cronSchedule, string name)
	{
		Job = job ?? throw new ArgumentNullException(nameof(job));
		CronSchedule = cronSchedule ?? throw new ArgumentNullException(nameof(cronSchedule));
		Name = name ?? throw new ArgumentNullException(nameof(name));
		CalculateNextRun();
	}

	public void CalculateNextRun()
	{
		NextRun = CronSchedule.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Local);
	}
}