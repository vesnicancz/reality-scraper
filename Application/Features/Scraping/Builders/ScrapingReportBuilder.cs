using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.Scraping.Builders;

public class ScrapingReportBuilder
{
	private Guid scraperTaskId;
	private string scraperTaskName = string.Empty;
	private readonly Dictionary<string, ScraperResultBuilder> scraperBuilders = new();
	private readonly HashSet<string> processedListings = new(); // Prevence duplikátů

	private readonly IListingRepository listingRepository;
	private readonly IDateTimeProvider dateTimeProvider;
	private readonly ILogger<ScrapingReportBuilder> logger;

	public ScrapingReportBuilder(
		IListingRepository listingRepository,
		IDateTimeProvider dateTimeProvider,
		ILogger<ScrapingReportBuilder> logger)
	{
		this.listingRepository = listingRepository;
		this.dateTimeProvider = dateTimeProvider;
		this.logger = logger;
	}

	public ScrapingReportBuilder ForScrapingReport(Guid taskId, string taskName)
	{
		scraperTaskId = taskId;
		scraperTaskName = taskName;
		scraperBuilders.Clear();
		processedListings.Clear();
		return this;
	}

	/// <summary>
	/// Batch zpracování všech listingů z jednoho scraperu
	/// </summary>
	public async Task<ScrapingReportBuilder> ProcessScraperResultsAsync(string siteName, List<ScraperListingItem> listings, CancellationToken cancellationToken)
	{
		logger.LogInformation("Zpracovávám {Count} listingů z {SiteName}", listings.Count, siteName);

		var builder = GetOrCreateScraperBuilder(siteName, listings.Count);

		foreach (var listing in listings)
		{
			await ProcessListingAsync(builder, siteName, listing, cancellationToken);
		}

		return this;
	}

	/// <summary>
	/// Zpracuje listing a automaticky určí typ operace (nový/změna ceny/existující)
	/// </summary>
	private async Task<ScrapingReportBuilder> ProcessListingAsync(ScraperResultBuilder builder, string siteName, ScraperListingItem listing, CancellationToken cancellationToken)
	{
		// Prevence duplikátů mezi scrapery
		var listingKey = listing.ExternalId;
		if (processedListings.Contains(listingKey))
		{
			logger.LogDebug("Duplikátní listing {ExternalId} přeskočen", listing.ExternalId);
			return this;
		}

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
		}

		processedListings.Add(listingKey);
		return this;
	}

	private ScraperResultBuilder GetOrCreateScraperBuilder(string siteName, int listingsCount)
	{
		if (!scraperBuilders.ContainsKey(siteName))
		{
			scraperBuilders[siteName] = new ScraperResultBuilder(siteName, listingsCount);
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
			ReportDate = dateTimeProvider.ToApplicationTime(dateTimeProvider.UtcNow),
			ScraperTaskId = scraperTaskId,
			TaskName = scraperTaskName,
			Results = results
		};
	}
}