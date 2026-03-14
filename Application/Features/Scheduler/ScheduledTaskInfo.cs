using RealityScraper.Application.Features.Scraping.Configuration;

namespace RealityScraper.Application.Features.Scheduler;

public class ScheduledTaskInfo
{
	public Guid Id { get; set; }

	public required string Name { get; set; }

	public required string CronExpression { get; set; }

	public required ScrapingConfiguration ScrapingConfiguration { get; set; }

	public DateTimeOffset? NextRunTime { get; set; }

	public bool IsRunning { get; set; }

	public DateTimeOffset? LastRunTime { get; set; }
}