using RealityScraper.Model;

namespace RealityScraper.Scraping.Scrapers;

public interface IRealityScraperService
{
	Task<List<Listing>> ScrapeListingsAsync();
}