using RealityScraper.Data;
using RealityScraper.Mailing;
using RealityScraper.Model;
using RealityScraper.Scheduler;
using RealityScraper.Scraping.Scrapers;

namespace RealityScraper.Scraping;

// Hlavní služba běžící na pozadí
public class ScraperServiceJob : IJob
{
	private readonly ILogger<ScraperServiceJob> logger;
	private readonly IServiceProvider serviceProvider;
	private readonly IConfiguration configuration;
	private readonly IEnumerable<IRealityScraperService> scraperServices;
	private readonly IEmailService emailService;
	private readonly RealityDbContext realityDbContext;
	private readonly IListingRepository listingRepository;

	public ScraperServiceJob(
		ILogger<ScraperServiceJob> logger,
		IServiceProvider serviceProvider,
		IConfiguration configuration,
		IEnumerable<IRealityScraperService> scraperServices,
		IEmailService emailService,
		RealityDbContext realityDbContext,
		IListingRepository listingRepository
		)
	{
		this.logger = logger;
		this.serviceProvider = serviceProvider;
		this.configuration = configuration;
		this.scraperServices = scraperServices;
		this.emailService = emailService;
		this.realityDbContext = realityDbContext;
		this.listingRepository = listingRepository;
	}

	public async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		// Načtení a zpracování dat
		var listings = new List<Listing>();
		foreach (var scraperService in scraperServices)
		{
			logger.LogInformation("Spouštím scraper: {scraperName}", scraperService.GetType().Name);
			listings.AddRange(await scraperService.ScrapeListingsAsync());
		}

		var newListings = new List<Listing>();

		foreach (var listing in listings)
		{
			var existingListing = await listingRepository.GetByExternalIdAsync(listing.ExternalId);
			if (existingListing == null)
			{
				listing.DiscoveredAt = DateTime.UtcNow;
				newListings.Add(listing);
				realityDbContext.Listings.Add(listing);
			}
			else
			{
				existingListing.LastSeenAt = DateTime.UtcNow;
			}
		}

		if (newListings.Any())
		{
			logger.LogInformation("Nalezeno {count} nových inzerátů.", newListings.Count);
			await realityDbContext.SaveChangesAsync(cancellationToken);
			await emailService.SendEmailNotificationAsync(newListings);
		}
		else
		{
			logger.LogInformation("Žádné nové inzeráty nebyly nalezeny.");
		}
	}
}