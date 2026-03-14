using Microsoft.Extensions.Logging;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Features.Scraping.Model.Report;

namespace RealityScraper.Application.Features.Scraping;

public class ScrapingReportProcessor : IScrapingReportProcessor
{
	private readonly IListingChangeProcessor listingChangeProcessor;
	private readonly IListingNotificationService notificationService;
	private readonly IListingImageDownloader imageDownloader;
	private readonly IUnitOfWork unitOfWork;
	private readonly ILogger<ScrapingReportProcessor> logger;

	public ScrapingReportProcessor(
		IListingChangeProcessor listingChangeProcessor,
		IListingNotificationService notificationService,
		IListingImageDownloader imageDownloader,
		IUnitOfWork unitOfWork,
		ILogger<ScrapingReportProcessor> logger)
	{
		this.listingChangeProcessor = listingChangeProcessor;
		this.notificationService = notificationService;
		this.imageDownloader = imageDownloader;
		this.unitOfWork = unitOfWork;
		this.logger = logger;
	}

	public async Task ProcessReportAsync(ScrapingReport report, List<string> emailRecipients, CancellationToken cancellationToken)
	{
		var listingsToDownload = await listingChangeProcessor.ProcessChangesAsync(report, cancellationToken);

		await notificationService.SendNotificationsAsync(report, emailRecipients, cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		await imageDownloader.DownloadImagesAsync(listingsToDownload, cancellationToken);
	}
}