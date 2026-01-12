using RealityScraper.Application.Features.Scraping.Model.Report;

namespace RealityScraper.Application.Interfaces.Mailing;

public interface IMailerService
{
	Task SendNewListingsAsync(ScrapingReport scrapingReport, List<string> recipients, CancellationToken cancellationToken);
}