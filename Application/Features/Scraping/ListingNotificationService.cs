using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces.Mailing;

namespace RealityScraper.Application.Features.Scraping;

public class ListingNotificationService : IListingNotificationService
{
	private readonly IMailerService mailerService;
	private readonly ILogger<ListingNotificationService> logger;

	public ListingNotificationService(
		IMailerService mailerService,
		ILogger<ListingNotificationService> logger)
	{
		this.mailerService = mailerService;
		this.logger = logger;
	}

	public async Task SendNotificationsAsync(ScrapingReport report, List<string> recipients, CancellationToken cancellationToken)
	{
		if (report.NewListingsCount > 0 || report.PriceChangedListingsCount > 0)
		{
			await mailerService.SendListingReportAsync(report, recipients, cancellationToken);
			logger.LogInformation("Notifikace odeslána.");
		}
		else
		{
			logger.LogDebug("Žádné nové inzeráty nebyly nalezeny.");
		}
	}
}