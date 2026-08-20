using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.Scraping;

public class ListingChangeProcessor : IListingChangeProcessor
{
	// Limity sloupců podle ListingConfiguration - delší hodnota by shodila uložení celého běhu.
	private const int MaxImageUrlLength = 500;
	private const int MaxTitleLength = 300;
	private const int MaxLocationLength = 300;
	private const int MaxUrlLength = 500;

	private readonly IListingRepository listingRepository;
	private readonly IDateTimeProvider dateTimeProvider;
	private readonly ILogger<ListingChangeProcessor> logger;

	public ListingChangeProcessor(
		IListingRepository listingRepository,
		IDateTimeProvider dateTimeProvider,
		ILogger<ListingChangeProcessor> logger)
	{
		this.listingRepository = listingRepository;
		this.dateTimeProvider = dateTimeProvider;
		this.logger = logger;
	}

	public async Task<List<Listing>> ProcessChangesAsync(ScrapingReport report, CancellationToken cancellationToken)
	{
		var listingsToDownload = new List<Listing>();
		var now = dateTimeProvider.UtcNow;

		// Existující inzeráty se načtou kvůli cenovým změnám i kvůli obnově údajů z portálu.
		Dictionary<string, Listing>? existingByExternalId = null;
		if (report.SeenListings.Count > 0 || report.Results.Any(r => r.PriceChangedListings.Any()))
		{
			existingByExternalId = (await listingRepository.GetByScraperTaskIdAsync(report.ScraperTaskId, cancellationToken))
				.ToDictionary(l => l.ExternalId);
		}

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
						CreatedAt = now,
						LastSeenAt = now,
						PriceFrom = now
					};
					listingRepository.Add(listing);
					listingsToDownload.Add(listing);
				}
			}
			if (result.PriceChangedListings.Any())
			{
				foreach (var priceChanged in result.PriceChangedListings)
				{
					if (existingByExternalId == null || !existingByExternalId.TryGetValue(priceChanged.ExternalId, out var existingListing))
					{
						continue;
					}

					existingListing.PriceHistories.Add(new PriceHistory
					{
						Price = existingListing.Price,
						RecordedAt = existingListing.PriceFrom
					});
					existingListing.Price = priceChanged.Price;
					existingListing.LastSeenAt = now;
					existingListing.PriceFrom = now;
				}
			}
		}

		var refreshedCount = 0;
		var imageChangedCount = 0;

		if (existingByExternalId != null)
		{
			foreach (var seen in report.SeenListings.Values)
			{
				// Nově vkládané inzeráty v mapě nejsou (načetla se před jejich vložením),
				// takže se jejich obrázek nezařadí ke stažení podruhé.
				if (!existingByExternalId.TryGetValue(seen.ExternalId, out var existingListing))
				{
					continue;
				}

				var refreshed = TryRefreshImageUrl(existingListing, seen, out var imageChanged);

				if (TryGetRefreshedText(existingListing.Title, seen.Title, MaxTitleLength, out var title))
				{
					existingListing.Title = title;
					refreshed = true;
				}

				if (TryGetRefreshedText(existingListing.Location, seen.Location, MaxLocationLength, out var location))
				{
					existingListing.Location = location;
					refreshed = true;
				}

				if (TryGetRefreshedText(existingListing.Url, seen.Url, MaxUrlLength, out var url))
				{
					existingListing.Url = url;
					refreshed = true;
				}

				if (!refreshed)
				{
					continue;
				}

				refreshedCount++;
				if (imageChanged)
				{
					imageChangedCount++;
					listingsToDownload.Add(existingListing);
				}
			}
		}

		logger.LogInformation("Zpracováno {NewCount} nových a {ChangedCount} cenově změněných listingů.", report.NewListingsCount, report.PriceChangedListingsCount);

		if (refreshedCount > 0)
		{
			logger.LogInformation("Obnoveny údaje u {RefreshedCount} z {SeenCount} viděných inzerátů, u {ImageChangedCount} se změnil obrázek a stáhne se znovu.",
				refreshedCount, report.SeenListings.Count, imageChangedCount);
		}

		// Vysoký podíl znovu stahovaných obrázků znamená nestabilní URL na portálu (podepsané odkazy,
		// náhodný uzel v cestě) - pak je potřeba upravit GetImageIdentity, jinak se každý běh
		// stahují všechny obrázky znovu.
		if (report.SeenListings.Count >= 10 && imageChangedCount > report.SeenListings.Count / 2)
		{
			logger.LogWarning("Úloha '{TaskName}': obrázek se mění u většiny inzerátů ({ImageChangedCount} z {SeenCount}), zkontrolujte stabilitu URL z portálu.",
				report.TaskName, imageChangedCount, report.SeenListings.Count);
		}

		return listingsToDownload;
	}

	/// <summary>
	/// Přepíše uloženou URL titulní fotky čerstvě nascrapovanou hodnotou. Vrací true, pokud se URL
	/// změnila; <paramref name="imageChanged"/> říká, zda jde o jiný obrázek (nutné stáhnout znovu),
	/// nebo jen o jinou variantu téže fotky (jiný CDN uzel, jiné parametry zmenšení).
	/// </summary>
	private bool TryRefreshImageUrl(Listing listing, ScraperListingItem seen, out bool imageChanged)
	{
		imageChanged = false;

		// Prázdná hodnota znamená selhaný selektor - dobrou uloženou URL přepsat nesmí.
		if (string.IsNullOrWhiteSpace(seen.ImageUrl))
		{
			return false;
		}

		if (seen.ImageUrl.Length > MaxImageUrlLength)
		{
			logger.LogWarning("URL obrázku inzerátu {ExternalId} přesahuje {MaxLength} znaků, obnova se přeskakuje.", seen.ExternalId, MaxImageUrlLength);
			return false;
		}

		if (string.Equals(listing.ImageUrl, seen.ImageUrl, StringComparison.Ordinal))
		{
			return false;
		}

		imageChanged = !string.Equals(GetImageIdentity(listing.ImageUrl), GetImageIdentity(seen.ImageUrl), StringComparison.Ordinal);

		logger.LogDebug("Inzerát {ExternalId}: URL titulní fotky změněna z '{OldImageUrl}' na '{NewImageUrl}' (jiný obrázek: {ImageChanged}).",
			seen.ExternalId, listing.ImageUrl, seen.ImageUrl, imageChanged);

		listing.ImageUrl = seen.ImageUrl;
		return true;
	}

	/// <summary>
	/// Vrátí true a přes <paramref name="refreshed"/> novou hodnotu, pokud se čerstvě nascrapovaný
	/// text liší od uloženého. Prázdnou hodnotou (selhaný selektor) ani hodnotou nad limit sloupce
	/// se uložený text nepřepisuje.
	/// </summary>
	private static bool TryGetRefreshedText(string stored, string fresh, int maxLength, out string refreshed)
	{
		refreshed = stored;

		// Whitespace z markupu by jinak "měnil" hodnotu při každém běhu.
		var trimmed = fresh.Trim();
		if (trimmed.Length == 0 || trimmed.Length > maxLength || string.Equals(stored, trimmed, StringComparison.Ordinal))
		{
			return false;
		}

		refreshed = trimmed;
		return true;
	}

	/// <summary>
	/// Identita obrázku pro porovnání = cesta bez hostitele a bez query. Portály přehazují inzeráty
	/// mezi CDN uzly a mění parametry zmenšení, samotný obrázek přitom zůstává stejný - bez tohoto
	/// ořezu by se každý běh stahovaly všechny obrázky znovu.
	/// </summary>
	private static string GetImageIdentity(string imageUrl)
	{
		return Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ? uri.AbsolutePath : imageUrl;
	}
}