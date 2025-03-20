using RealityScraper.Scraping.Model;

namespace RealityScraper.Mailing;

public interface IHtmlMailGenerator
{
	string GenerateHtmlBody(ScrapingReport scrapingReport);
}