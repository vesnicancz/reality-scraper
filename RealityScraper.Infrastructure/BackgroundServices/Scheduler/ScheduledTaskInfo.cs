using Cronos;
using RealityScraper.Application.Features.Scraping.Configuration;

namespace RealityScraper.Infrastructure.BackgroundServices.Scheduler;

public class ScheduledTaskInfo
{
	public string Id { get; set; } // ID úlohy v databázi

	public string Name { get; set; }

	public CronExpression CronExpression { get; set; }

	public ScrapingConfiguration ScrapingConfiguration { get; set; }

	public DateTime? NextRunTime { get; set; }

	public bool IsRunning { get; set; }

	public DateTime? LastRunTime { get; set; }
}