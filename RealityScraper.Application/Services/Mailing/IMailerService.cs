using RealityScraper.Application.Features.Scraping.Model;

namespace RealityScraper.Application.Services.Mailing;

public interface IMailerService
{
	Task SendNewListingsAsync(ScrapingReport scrapingReport, List<string> recipients, CancellationToken cancellationToken);
}