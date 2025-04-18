using RealityScraper.Application.Features.Scraping.Configuration;

namespace RealityScraper.Infrastructure.Utilities.Configuration;

public class TaskConfiguration
{
	public string Name { get; set; }

	public string CronExpression { get; set; }

	public bool Enabled { get; set; } = true;

	public ScrapingConfiguration ScrapingConfiguration { get; set; }
}