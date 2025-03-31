using RealityScraper.Application.Features.Scheduling.Configuration;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Application.Features.Scraping.Scrapers;

public interface IRealityScraperService
{
	string SiteName { get; }

	ScrapersEnum ScrapersEnum { get; }

	Task<List<ListingItem>> ScrapeListingsAsync(ScraperConfiguration scraperConfiguration);
}