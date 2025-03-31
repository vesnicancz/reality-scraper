using RealityScraper.Application.Features.Scraping.Model;

namespace RealityScraper.Application.Interfaces.Mailing;

public interface IEmailGenerator
{
	Task<string> GenerateHtmlBodyAsync(ScrapingReport scrapingReport, CancellationToken cancellationToken);
}