using RealityScraper.Scheduler.Configuration;
using RealityScraper.Scraping.Model;

namespace RealityScraper.Scraping.Scrapers;

public interface IRealityScraperService
{
	string SiteName { get; }

	ScrapersEnum ScrapersEnum { get; }

	Task<List<ListingItem>> ScrapeListingsAsync(ScraperConfiguration scraperConfiguration);
}