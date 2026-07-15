using RealityScraper.Application.Features.Reporting.Model;
using RealityScraper.Application.Features.Scraping.Model.Report;

namespace RealityScraper.Application.Interfaces.Mailing;

public interface IEmailGenerator
{
	Task<string> GenerateHtmlBodyAsync(ScrapingReport scrapingReport, CancellationToken cancellationToken);

	Task<string> GenerateRemovedListingsHtmlAsync(RemovedListingsReport report, CancellationToken cancellationToken);
}