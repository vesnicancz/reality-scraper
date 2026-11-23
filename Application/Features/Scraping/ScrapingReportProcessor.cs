using Microsoft.Extensions.Logging;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Diagnostics;
using RealityScraper.Application.Features.Scraping.Model.Report;

namespace RealityScraper.Application.Features.Scraping;

public class ScrapingReportProcessor : IScrapingReportProcessor
{
	private readonly IListingChangeProcessor listingChangeProcessor;
	private readonly IRemovedListingDetector removedListingDetector;
	private readonly IListingNotificationService notificationService;
	private readonly IListingImageDownloader imageDownloader;
	private readonly IUnitOfWork unitOfWork;
	private readonly ILogger<ScrapingReportProcessor> logger;

	public ScrapingReportProcessor(
		IListingChangeProcessor listingChangeProcessor,
		IRemovedListingDetector removedListingDetector,
		IListingNotificationService notificationService,
		IListingImageDownloader imageDownloader,
		IUnitOfWork unitOfWork,
		ILogger<ScrapingReportProcessor> logger)
	{
		this.listingChangeProcessor = listingChangeProcessor;
		this.removedListingDetector = removedListingDetector;
		this.notificationService = notificationService;
		this.imageDownloader = imageDownloader;
		this.unitOfWork = unitOfWork;
		this.logger = logger;
	}

	public async Task ProcessReportAsync(ScrapingReport report, List<string> emailRecipients, CancellationToken cancellationToken)
	{
		using var activity = ScraperTelemetry.ActivitySource.StartActivity("Process report");
		activity?.SetTag("report.listings.total", report.TotalListingsCount);
		activity?.SetTag("report.listings.new", report.NewListingsCount);
		activity?.SetTag("report.listings.price_changed", report.PriceChangedListingsCount);
		activity?.SetTag("report.scraping_succeeded", report.ScrapingSucceeded);

		logger.LogInformation("Zpracovávám report úlohy '{TaskName}'", report.TaskName);

		var listingsToDownload = await listingChangeProcessor.ProcessChangesAsync(report, cancellationToken);

		await removedListingDetector.DetectAsync(report, cancellationToken);

		await notificationService.SendNotificationsAsync(report, emailRecipients, cancellationToken);

		await unitOfWork.SaveChangesAsync(cancellationToken);

		// Až po uložení - při selhání notifikace se změny neuloží a další běh je nahlásí znovu,
		// takže dřívější započítání by je v metrice zdvojilo.
		RecordListingChanges(report);

		await imageDownloader.DownloadImagesAsync(listingsToDownload, cancellationToken);

		logger.LogInformation("Report úlohy '{TaskName}' zpracován", report.TaskName);
	}

	private static void RecordListingChanges(ScrapingReport report)
	{
		foreach (var portal in report.Results)
		{
			var siteTag = new KeyValuePair<string, object?>(ScraperTelemetry.Tags.Site, portal.SiteName);
			ScraperTelemetry.ListingChanges.Add(portal.NewListings.Count, siteTag, new(ScraperTelemetry.Tags.ListingChange, "new"));
			ScraperTelemetry.ListingChanges.Add(portal.PriceChangedListings.Count, siteTag, new(ScraperTelemetry.Tags.ListingChange, "price_changed"));
		}
	}
}
