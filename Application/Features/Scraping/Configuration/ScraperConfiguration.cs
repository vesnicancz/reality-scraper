using RealityScraper.Domain.Enums;

namespace RealityScraper.Application.Features.Scraping.Configuration;

public class ScraperConfiguration
{
	public ScrapersEnum ScraperType { get; set; }

	public required string Url { get; set; }
}