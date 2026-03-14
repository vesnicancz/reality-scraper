using RealityScraper.Application.Features.Scraping.Model.Report;

namespace RealityScraper.Application.Features.Scraping;

public interface IListingNotificationService
{
	Task SendNotificationsAsync(ScrapingReport report, List<string> recipients, CancellationToken cancellationToken);
}