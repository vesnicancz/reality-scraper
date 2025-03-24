using RealityScraper.Scraping.Scrapers;

namespace RealityScraper.Scheduler.Configuration;

public class ScraperConfiguration
{
	public ScrapersEnum ScraperType { get; set; }

	public string Url { get; set; }
}