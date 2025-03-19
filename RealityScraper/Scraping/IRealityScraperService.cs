using RealityScraper.Model;

namespace RealityScraper.Scraping;

public interface IRealityScraperService
{
	Task<List<Listing>> ScrapeListingsAsync();
}