using RealityScraper.Data;
using RealityScraper.Mailing;
using RealityScraper.Model;

namespace RealityScraper.Scraping;

// Hlavní služba běžící na pozadí
public class ScraperService : BackgroundService
{
	private readonly ILogger<ScraperService> logger;
	private readonly IServiceProvider serviceProvider;
	private readonly IConfiguration configuration;

	public ScraperService(
		ILogger<ScraperService> logger,
		IServiceProvider serviceProvider,
		IConfiguration configuration
		)
	{
		this.logger = logger;
		this.serviceProvider = serviceProvider;
		this.configuration = configuration;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			logger.LogInformation("Spouštím kontrolu nových inzerátů: {time}", DateTimeOffset.Now);

			try
			{
				// Vytvoříme scope pro DI
				using (var scope = serviceProvider.CreateScope())
				{
					var scraperService = scope.ServiceProvider.GetRequiredService<IRealityScraperService>();
					var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
					var dbContext = scope.ServiceProvider.GetRequiredService<RealityDbContext>();
					var realityListingRepository = scope.ServiceProvider.GetRequiredService<IListingRepository>();

					// Načtení a zpracování dat
					var listings = await scraperService.ScrapeListingsAsync();
					var newListings = new List<Listing>();

					foreach (var listing in listings)
					{
						var existingListing = await realityListingRepository.GetByExternalIdAsync(listing.ExternalId);
						if (existingListing == null)
						{
							listing.DiscoveredAt = DateTime.UtcNow;
							newListings.Add(listing);
							dbContext.Listings.Add(listing);
						}
						else
						{
							existingListing.LastSeenAt = DateTime.UtcNow;
						}
					}

					if (newListings.Any())
					{
						logger.LogInformation("Nalezeno {count} nových inzerátů.", newListings.Count);
						await dbContext.SaveChangesAsync(stoppingToken);
						await emailService.SendEmailNotificationAsync(newListings);
					}
					else
					{
						logger.LogInformation("Žádné nové inzeráty nebyly nalezeny.");
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Došlo k chybě při zpracování inzerátů.");
			}

			// Interval kontroly z konfigurace (výchozí 1 hodina)
			var interval = configuration.GetValue("ScraperSettings:IntervalMinutes", 60);
			await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
		}
	}
}