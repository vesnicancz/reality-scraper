namespace RealityScraper.Persistence.Seeding.Configuration;

public class ScrapingConfiguration
{
	public List<string> EmailRecipients { get; set; } = new List<string>();

	public List<ScraperConfiguration> Scrapers { get; set; } = new List<ScraperConfiguration>();
}