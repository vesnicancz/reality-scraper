using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.Scraping.Builders;

public class ScrapingReportBuilder
{
	private Guid scraperTaskId;
	private string scraperTaskName = string.Empty;
	private bool allScrapersSucceeded = true;
	private int failedListingsCount;
	private bool anyTargetEmpty;
	private Dictionary<string, Listing>? existingListingsByExternalId;
	private readonly Dictionary<string, ScraperResultBuilder> scraperBuilders = new();
	private readonly HashSet<string> processedListings = new(); // Klíč "siteName|externalId" - prevence duplikátů mezi targety téhož portálu
	private readonly HashSet<string> seenExternalIds = new();

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
		allScrapersSucceeded = true;
		failedListingsCount = 0;
		anyTargetEmpty = false;
		existingListingsByExternalId = null;
		scraperBuilders.Clear();
		processedListings.Clear();
		seenExternalIds.Clear();
		return this;
	}

	/// <summary>
	/// Označí běh scrapování jako neúspěšný (např. scraper nebyl nalezen nebo selhal).
	/// </summary>
	public ScrapingReportBuilder MarkScraperFailed()
	{
		allScrapersSucceeded = false;
		return this;
	}

	/// <summary>
	/// Batch zpracování všech listingů z jednoho scraperu
	/// </summary>
	public async Task<ScrapingReportBuilder> ProcessScraperResultsAsync(string siteName, ScraperRunResult scraperResult, CancellationToken cancellationToken)
	{
		var listings = scraperResult.Listings;
		logger.LogInformation("Zpracovávám {Count} listingů z {SiteName}", listings.Count, siteName);

		if (!scraperResult.Success)
		{
			allScrapersSucceeded = false;
		}

		failedListingsCount += scraperResult.FailedListingsCount;

		if (scraperResult.Success && listings.Count == 0)
		{
			anyTargetEmpty = true;
		}

		var builder = GetOrCreateScraperBuilder(siteName);

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
		// Prevence duplikátů mezi targety téhož portálu
		var listingKey = $"{siteName}|{listing.ExternalId}";
		if (processedListings.Contains(listingKey))
		{
			logger.LogDebug("Duplikátní listing {ExternalId} přeskočen", listing.ExternalId);
			return this;
		}

		// Kontrola existence v databázi (všechny inzeráty úlohy se načtou jedním dotazem)
		existingListingsByExternalId ??= (await listingRepository.GetByScraperTaskIdAsync(scraperTaskId, cancellationToken))
			.ToDictionary(l => l.ExternalId);
		existingListingsByExternalId.TryGetValue(listing.ExternalId, out var existingListing);

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
		else if (listing.Price == null && existingListing.Price != null)
		{
			// "Cena na dotaz" apod. - nejde o změnu ceny, uložená cena se nepřepisuje
			logger.LogInformation("Listing {ExternalId}: cena se nepodařila naparsovat, ponechává se uložená hodnota {Price}.", listing.ExternalId, existingListing.Price);
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

		builder.IncrementTotalCount();
		processedListings.Add(listingKey);
		seenExternalIds.Add(listing.ExternalId);
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
			ReportDate = dateTimeProvider.ToApplicationTime(dateTimeProvider.UtcNow),
			ScraperTaskId = scraperTaskId,
			TaskName = scraperTaskName,
			Results = results,
			ScrapingSucceeded = allScrapersSucceeded,
			SeenExternalIds = new HashSet<string>(seenExternalIds),
			FailedListingsCount = failedListingsCount,
			AnyTargetEmpty = anyTargetEmpty
		};
	}
}