using Cronos;
using RealityScraper.Application.Features.Scheduling.Configuration;

namespace RealityScraper.Infrastructure.BackgroundServices.Scheduler;

public class ScheduledTaskInfo
{
	public string Name { get; set; }

	public CronExpression CronExpression { get; set; }

	public ScrapingConfiguration ScrapingConfiguration { get; set; }

	public DateTime? NextRunTime { get; set; }

	public bool IsRunning { get; set; }

	public DateTime? LastRunTime { get; set; }
}