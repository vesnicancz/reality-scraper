using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces;
using RealityScraper.Application.Interfaces.Mailing;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Application.Features.Scraping;

public class ScrapingReportProcessor : IScrapingReportProcessor
{
	private readonly IListingRepository listingRepository;
	private readonly IMailerService mailerService;
	private readonly IImageDownloadService imageDownloadService;
	private readonly IUnitOfWork unitOfWork;
	private readonly ILogger<ScrapingReportProcessor> logger;

	public ScrapingReportProcessor(
		IListingRepository listingRepository,
		IMailerService mailerService,
		IImageDownloadService imageDownloadService,
		IUnitOfWork unitOfWork,
		ILogger<ScrapingReportProcessor> logger)
	{
		this.listingRepository = listingRepository;
		this.mailerService = mailerService;
		this.imageDownloadService = imageDownloadService;
		this.unitOfWork = unitOfWork;
		this.logger = logger;
	}

	public async Task ProcessReportAsync(ScrapingReport report, List<string> emailRecipients, CancellationToken cancellationToken)
	{
		// Procesování změn
		var listingsToDownload = await ProcessChangesAsync(report, cancellationToken);

		// Odeslání notifikací
		await SendNotificationsAsync(report, emailRecipients, cancellationToken);

		// Uložení změn do databáze
		await unitOfWork.SaveChangesAsync(cancellationToken);

		// Stáhnutí obrázků
		await DownloadListingImagesAsync(listingsToDownload, cancellationToken);
	}

	private async Task<List<Listing>> ProcessChangesAsync(ScrapingReport report, CancellationToken cancellationToken)
	{
		var listingsToDownload = new List<Listing>();

		foreach (var result in report.Results)
		{
			if (result.NewListings.Any())
			{
				foreach (var newListing in result.NewListings)
				{
					var listing = new Listing
					{
						Title = newListing.Title,
						Price = newListing.Price,
						Location = newListing.Location,
						Url = newListing.Url,
						ImageUrl = newListing.ImageUrl,
						ScraperTaskId = report.ScraperTaskId,
						ExternalId = newListing.ExternalId,
						CreatedAt = DateTime.UtcNow,
						LastSeenAt = DateTime.UtcNow,
						PriceFrom = DateTime.UtcNow
					};
					await listingRepository.AddAsync(listing, cancellationToken);
					listingsToDownload.Add(listing);
				}
			}
			if (result.PriceChangedListings.Any())
			{
				foreach (var priceChanged in result.PriceChangedListings)
				{
					var existingListing = await listingRepository.GetByExternalIdAsync(report.ScraperTaskId, priceChanged.ExternalId, cancellationToken);
					if (existingListing != null)
					{
						existingListing.PriceHistories.Add(new PriceHistory
						{
							Price = existingListing.Price,
							RecordedAt = existingListing.PriceFrom
						});
						existingListing.Price = priceChanged.Price;
						existingListing.LastSeenAt = DateTime.UtcNow;
						existingListing.PriceFrom = DateTime.UtcNow;
					}
				}
			}
		}

		return listingsToDownload;
	}

	private async Task SendNotificationsAsync(ScrapingReport report, List<string> recipients, CancellationToken cancellationToken)
	{
		// Odeslání notifikace
		if (report.NewListingsCount > 0 || report.PriceChangedListingsCount > 0)
		{
			logger.LogInformation("Nalezeno {newCount} nových inzerátů a {priceChangedCount} upravených cen.", report.NewListingsCount, report.PriceChangedListingsCount);
			await mailerService.SendNewListingsAsync(report, recipients, cancellationToken);
		}
		else
		{
			logger.LogInformation("Žádné nové inzeráty nebyly nalezeny.");
		}
	}

	private async Task DownloadListingImagesAsync(List<Listing> listingsToDownload, CancellationToken cancellationToken)
	{
		foreach (var listing in listingsToDownload)
		{
			await imageDownloadService.DownloadImageAsync(listing, cancellationToken);
		}
	}
}