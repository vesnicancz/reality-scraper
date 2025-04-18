using RealityScraper.Domain.Enums;

namespace RealityScraper.Persistence.Seeding.Configuration;

public class ScraperConfiguration
{
	public ScrapersEnum ScraperType { get; set; }

	public string Url { get; set; }
}