using RealityScraper.Application.Features.Scraping.Configuration;

namespace RealityScraper.Infrastructure.BackgroundServices.Scheduler;

public class ScheduledTaskInfo
{
	public Guid Id { get; set; }

	public string Name { get; set; }

	public string CronExpression { get; set; }

	public ScrapingConfiguration ScrapingConfiguration { get; set; }

	public DateTime? NextRunTime { get; set; }

	public bool IsRunning { get; set; }

	public DateTime? LastRunTime { get; set; }
}