using RealityScraper.Data;
using RealityScraper.Mailing;
using RealityScraper.Model;
using RealityScraper.Scheduler;
using RealityScraper.Scheduler.Configuration;
using RealityScraper.Scraping.Model;
using RealityScraper.Scraping.Scrapers;

namespace RealityScraper.Scraping;

// Hlavní služba běžící na pozadí
public class ScraperServiceJob : IScheduledTask
{
	private readonly IEnumerable<IRealityScraperService> scraperServices;
	private readonly IEmailService emailService;
	private readonly IImageDownloadService imageDownloadService;
	private readonly IServiceProvider serviceProvider;
	private readonly ILogger<ScraperServiceJob> logger;

	public ScraperServiceJob(
		IEnumerable<IRealityScraperService> scraperServices,
		IEmailService emailService,
		IImageDownloadService imageDownloadService,
		IServiceProvider serviceProvider,
		ILogger<ScraperServiceJob> logger
		)
	{
		this.scraperServices = scraperServices;
		this.emailService = emailService;
		this.imageDownloadService = imageDownloadService;
		this.serviceProvider = serviceProvider;
		this.logger = logger;
	}

	public async Task ExecuteAsync(ScrapingConfiguration configuration, CancellationToken cancellationToken)
	{
		// Načtení a zpracování dat
		var report = new ScrapingReport();

		using var scope = serviceProvider.CreateScope();
		var listingRepository = scope.ServiceProvider.GetRequiredService<IListingRepository>();
		var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

		var listingsToDownload = new List<Listing>();

		var scrapersDictionary = scraperServices.ToDictionary(i => i.ScrapersEnum);

		foreach (var scraperConfiguration in configuration.Scrapers)
		{
			if (!scrapersDictionary.TryGetValue(scraperConfiguration.ScraperType, out var scraperService))
			{
				logger.LogWarning("Scraper '{scraperName}' not found.", scraperConfiguration.ScraperType);
				continue;
			}

			logger.LogInformation("Spouštím scraper: {scraperName}", scraperService.SiteName);
			var listings = await scraperService.ScrapeListingsAsync(scraperConfiguration);

			var scraperResult = new ScraperResult
			{
				SiteName = scraperService.SiteName,
				TotalListingCount = listings.Count
			};
			report.Results.Add(scraperResult);

			foreach (var listing in listings)
			{
				var existingListing = await listingRepository.GetByExternalIdAsync(listing.ExternalId, cancellationToken);
				if (existingListing == null)
				{
					// Nový inzerát
					var newListing = new Listing
					{
						Title = listing.Title,
						Price = listing.Price,
						Location = listing.Location,
						Url = listing.Url,
						ImageUrl = listing.ImageUrl,
						ExternalId = listing.ExternalId,
						DiscoveredAt = DateTime.UtcNow,
						LastSeenAt = DateTime.UtcNow,
						PriceFrom = DateTime.UtcNow,
					};

					scraperResult.NewListings.Add(listing);
					await listingRepository.AddAsync(newListing, cancellationToken);
					listingsToDownload.Add(newListing);
				}
				else if (listing.Price != existingListing.Price)
				{
					// Změna ceny
					var oldPrice = existingListing.Price;

					existingListing.PriceHistories.Add(new PriceHistory
					{
						Price = existingListing.Price,
						RecordedAt = existingListing.PriceFrom
					});
					existingListing.Price = listing.Price;
					existingListing.LastSeenAt = DateTime.UtcNow;
					existingListing.PriceFrom = DateTime.UtcNow;

					scraperResult.PriceChangedListings.Add(
						new ListingItemWithNewPrice
						{
							Title = listing.Title,
							Price = listing.Price,
							Location = listing.Location,
							Url = listing.Url,
							ImageUrl = listing.ImageUrl,
							ExternalId = listing.ExternalId,
							OldPrice = oldPrice
						});
				}
				else
				{
					// Inzerát již existuje
					existingListing.LastSeenAt = DateTime.UtcNow;
				}
			}
		}

		// Odeslání notifikace
		if (report.NewListingCount > 0 || report.PriceChangedListingsCount > 0)
		{
			logger.LogInformation("Nalezeno {newCount} nových inzerátů a {priceChangedCount} upravených cen.", report.NewListingCount, report.PriceChangedListingsCount);
			await emailService.SendEmailNotificationAsync(report, configuration.EmailRecipients);
		}
		else
		{
			logger.LogInformation("Žádné nové inzeráty nebyly nalezeny.");
		}

		// Uložení změn do databáze
		await unitOfWork.SaveChangesAsync(cancellationToken);

		// Stáhnutí obrázků
		foreach (var listing in listingsToDownload)
		{
			await imageDownloadService.DownloadImageAsync(listing, cancellationToken);
		}
	}
}