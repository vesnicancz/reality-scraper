namespace RealityScraper.Scheduler;

// Rozhraní pro registraci úloh
public interface ISchedulerRegistry
{
	void AddJob<T>(string cronExpression, string jobName = null)
		where T : IJob;

	void AddJob(IJob job, string cronExpression, string jobName = null);

	IReadOnlyCollection<ScheduledJob> GetScheduledJobs();
}