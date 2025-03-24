using Cronos;
using RealityScraper.Scheduler.Configuration;

namespace RealityScraper.Scheduler;

// Základ pro plánovanou úlohu
public class ScheduledTaskInfo
{
	public string Name { get; set; }

	public CronExpression CronExpression { get; set; }

	public ScrapingConfiguration ScrapingConfiguration { get; set; }

	public DateTime? NextRunTime { get; set; }

	public bool IsRunning { get; set; }

	public DateTime? LastRunTime { get; set; }
}