using RealityScraper.Application.Features.Scraping.Configuration;

namespace RealityScraper.Application.Features.Scheduler;

public class ScheduledTaskInfo
{
	public Guid Id { get; set; }

	public string Name { get; set; }

	public string CronExpression { get; set; }

	public ScrapingConfiguration ScrapingConfiguration { get; set; }

	public DateTimeOffset? NextRunTime { get; set; }

	public bool IsRunning { get; set; }

	public DateTimeOffset? LastRunTime { get; set; }
}