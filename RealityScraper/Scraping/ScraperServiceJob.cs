using RealityScraper.Data;
using RealityScraper.Mailing;
using RealityScraper.Model;
using RealityScraper.Scheduler;
using RealityScraper.Scraping.Model;
using RealityScraper.Scraping.Scrapers;

namespace RealityScraper.Scraping;

// Hlavní služba běžící na pozadí
public class ScraperServiceJob : IJob
{
	private readonly ILogger<ScraperServiceJob> logger;
	private readonly IEnumerable<IRealityScraperService> scraperServices;
	private readonly IEmailService emailService;
	private readonly IServiceProvider serviceProvider;

	public ScraperServiceJob(
		ILogger<ScraperServiceJob> logger,
		IEnumerable<IRealityScraperService> scraperServices,
		IEmailService emailService,
		IServiceProvider serviceProvider
		)
	{
		this.logger = logger;
		this.scraperServices = scraperServices;
		this.emailService = emailService;
		this.serviceProvider = serviceProvider;
	}

	public async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		// Načtení a zpracování dat
		var report = new ScrapingReport();

		using var scope = serviceProvider.CreateScope();
		var realityDbContext = scope.ServiceProvider.GetRequiredService<RealityDbContext>();
		var listingRepository = scope.ServiceProvider.GetRequiredService<IListingRepository>();

		foreach (var scraperService in scraperServices)
		{
			logger.LogInformation("Spouštím scraper: {scraperName}", scraperService.SiteName);
			var listings = await scraperService.ScrapeListingsAsync();

			var scraperResult = new ScraperResult(scraperService.SiteName, listings.Count);
			report.Results.Add(scraperResult);

			foreach (var listing in listings)
			{
				var existingListing = await listingRepository.GetByExternalIdAsync(listing.ExternalId);
				if (existingListing == null)
				{
					var newListing = new Listing
					{
						Title = listing.Title,
						Price = listing.Price,
						Location = listing.Location,
						Url = listing.Url,
						ImageUrl = listing.ImageUrl,
						ExternalId = listing.ExternalId,
						DiscoveredAt = DateTime.UtcNow
					};

					scraperResult.NewListings.Add(listing);

					realityDbContext.Listings.Add(newListing);
				}
				else
				{
					existingListing.LastSeenAt = DateTime.UtcNow;
				}
			}
		}

		if (report.NewListingCount > 0)
		{
			logger.LogInformation("Nalezeno {count} nových inzerátů.", report.NewListingCount);
			await realityDbContext.SaveChangesAsync(cancellationToken);
			await emailService.SendEmailNotificationAsync(report);
		}
		else
		{
			logger.LogInformation("Žádné nové inzeráty nebyly nalezeny.");
		}
	}
}