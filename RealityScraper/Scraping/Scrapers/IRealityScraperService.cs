using RealityScraper.Scraping.Model;

namespace RealityScraper.Scraping.Scrapers;

public interface IRealityScraperService
{
	string SiteName { get; }

	Task<List<ListingItem>> ScrapeListingsAsync();
}