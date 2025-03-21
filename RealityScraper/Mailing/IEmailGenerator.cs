using RealityScraper.Scraping.Model;

namespace RealityScraper.Mailing;

public interface IEmailGenerator
{
	Task<string> GenerateHtmlBodyAsync(ScrapingReport scrapingReport);
}