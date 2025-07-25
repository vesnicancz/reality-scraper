using RealityScraper.Application.Features.Scraping.Model.Report;

namespace RealityScraper.Application.Interfaces.Mailing;

public interface IEmailGenerator
{
	Task<string> GenerateHtmlBodyAsync(ScrapingReport scrapingReport, CancellationToken cancellationToken);
}