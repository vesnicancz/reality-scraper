using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Features.Scraping.Model.Report;

namespace RealityScraper.Application.Features.Scraping;

public class ScrapingReportProcessor : IScrapingReportProcessor
{
	private readonly IListingChangeProcessor listingChangeProcessor;
	private readonly IListingNotificationService notificationService;
	private readonly IListingImageDownloader imageDownloader;
	private readonly IUnitOfWork unitOfWork;

	public ScrapingReportProcessor(
		IListingChangeProcessor listingChangeProcessor,
		IListingNotificationService notificationService,
		IListingImageDownloader imageDownloader,
		IUnitOfWork unitOfWork)
	{
		this.listingChangeProcessor = listingChangeProcessor;
		this.notificationService = notificationService;
		this.imageDownloader = imageDownloader;
		this.unitOfWork = unitOfWork;
	}

	public async Task ProcessReportAsync(ScrapingReport report, List<string> emailRecipients, CancellationToken cancellationToken)
	{
		var listingsToDownload = await listingChangeProcessor.ProcessChangesAsync(report, cancellationToken);

		await notificationService.SendNotificationsAsync(report, emailRecipients, cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		await imageDownloader.DownloadImagesAsync(listingsToDownload, cancellationToken);
	}
}