using RealityScraper.Application.Features.Scraping.Model.Report;

namespace RealityScraper.Application.Features.Scraping;
public interface IScrapingReportProcessor
{
	Task ProcessReportAsync(ScrapingReport report, List<string> emailRecipients, CancellationToken cancellationToken);
}