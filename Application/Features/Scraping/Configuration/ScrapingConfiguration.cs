namespace RealityScraper.Application.Features.Scraping.Configuration;

public class ScrapingConfiguration
{
	public Guid Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public List<string> EmailRecipients { get; set; } = new List<string>();

	public List<ScraperConfiguration> Scrapers { get; set; } = new List<ScraperConfiguration>();
}