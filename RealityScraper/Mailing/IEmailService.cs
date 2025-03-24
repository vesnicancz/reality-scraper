using RealityScraper.Scraping.Model;

namespace RealityScraper.Mailing;

public interface IEmailService
{
	Task SendEmailNotificationAsync(ScrapingReport scrapingReport, List<string> recipients);
}