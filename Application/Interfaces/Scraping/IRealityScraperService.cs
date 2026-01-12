using RealityScraper.Application.Features.Scraping.Configuration;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Application.Interfaces.Scraping;

public interface IRealityScraperService
{
	string SiteName { get; }

	ScrapersEnum ScrapersEnum { get; }

	Task<List<ScraperListingItem>> ScrapeListingsAsync(ScraperConfiguration scraperConfiguration, CancellationToken cancellationToken);
}