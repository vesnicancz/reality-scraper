using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces.Repositories.Realty;

namespace RealityScraper.Application.Features.Scraping.Builders;

public class ScrapingReportBuilder
{
	private Guid scraperTaskId;
	private readonly Dictionary<string, ScraperResultBuilder> scraperBuilders = new();
	private readonly HashSet<string> processedListings = new(); // Prevence duplikátů

	private readonly IListingRepository listingRepository;
	private readonly ILogger<ScrapingReportBuilder> logger;

	public ScrapingReportBuilder(
		IListingRepository listingRepository,
		ILogger<ScrapingReportBuilder> logger)
	{
		this.listingRepository = listingRepository;
		this.logger = logger;
	}

	public ScrapingReportBuilder ForScrapingReport(Guid taskId)
	{
		scraperTaskId = taskId;
		return this;
	}

	/// <summary>
	/// Inicializuje scraper pro daný web
	/// </summary>
	public ScrapingReportBuilder ForScraper(string siteName)
	{
		if (!scraperBuilders.ContainsKey(siteName))
		{
			scraperBuilders[siteName] = new ScraperResultBuilder(siteName);
		}
		return this;
	}

	/// <summary>
	/// Batch zpracování všech listingů z jednoho scraperu
	/// </summary>
	public async Task<ScrapingReportBuilder> ProcessScraperResultsAsync(string siteName, List<ScraperListingItem> listings, CancellationToken cancellationToken)
	{
		logger.LogInformation("Zpracovávám {Count} listingů z {SiteName}", listings.Count, siteName);

		foreach (var listing in listings)
		{
			await ProcessListingAsync(siteName, listing, cancellationToken);
		}

		return this;
	}

	/// <summary>
	/// Zpracuje listing a automaticky určí typ operace (nový/změna ceny/existující)
	/// </summary>
	private async Task<ScrapingReportBuilder> ProcessListingAsync(string siteName, ScraperListingItem listing, CancellationToken cancellationToken)
	{
		// Prevence duplikátů mezi scrapery
		var listingKey = listing.ExternalId;
		if (processedListings.Contains(listingKey))
		{
			logger.LogDebug("Duplikátní listing {ExternalId} přeskočen", listing.ExternalId);
			return this;
		}

		var builder = GetOrCreateScraperBuilder(siteName);

		// Kontrola existence v databázi
		var existingListing = await listingRepository.GetByExternalIdAsync(scraperTaskId, listing.ExternalId, cancellationToken);

		if (existingListing == null)
		{
			// Nový listing
			var newListing = new ListingItem
			{
				Title = listing.Title,
				Price = listing.Price,
				Location = listing.Location,
				Url = listing.Url,
				ImageUrl = listing.ImageUrl,
				ExternalId = listing.ExternalId
			};
			builder.AddNewListing(newListing);
			logger.LogDebug("Nový listing {ExternalId} přidán do {SiteName}", listing.ExternalId, siteName);
		}
		else if (listing.Price != existingListing.Price)
		{
			// Změna ceny
			var priceChangedListing = new ListingItemWithNewPrice
			{
				Title = listing.Title,
				Price = listing.Price,
				Location = listing.Location,
				Url = listing.Url,
				ImageUrl = listing.ImageUrl,
				ExternalId = listing.ExternalId,
				OldPrice = existingListing.Price
			};

			builder.AddPriceChangedListing(priceChangedListing);
			logger.LogDebug("Změna ceny u listingu {ExternalId}: {OldPrice} → {NewPrice}", listing.ExternalId, existingListing.Price, listing.Price);
		}
		else
		{
			// Existující bez změn - jen započítáme do celkového počtu
			// builder.IncrementTotalCount();
			// NOOP
		}

		processedListings.Add(listingKey);
		return this;
	}

	private ScraperResultBuilder GetOrCreateScraperBuilder(string siteName)
	{
		if (!scraperBuilders.ContainsKey(siteName))
		{
			scraperBuilders[siteName] = new ScraperResultBuilder(siteName);
		}
		return scraperBuilders[siteName];
	}

	/// <summary>
	/// Vytvoří finální report
	/// </summary>
	public ScrapingReport Build()
	{
		var results = scraperBuilders.Values
			.Select(builder => builder.Build())
			.ToList();

		return new ScrapingReport
		{
			ReportDate = DateTime.Now,
			ScraperTaskId = scraperTaskId,
			Results = results
		};
	}
}