using Microsoft.Extensions.Logging;
using RealityScraper.Application.Configuration;
using RealityScraper.Application.Features.Scraping.Configuration;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Application.Features.Scraping.Scrapers;

public abstract class BaseScraperService : IRealityScraperService
{
	protected readonly ILogger logger;
	protected readonly IWebDriverFactory webDriverFactory;
	private readonly IUrlSafetyValidator urlSafetyValidator;

	protected BaseScraperService(ILogger logger, IWebDriverFactory webDriverFactory, IUrlSafetyValidator urlSafetyValidator)
	{
		this.logger = logger;
		this.webDriverFactory = webDriverFactory;
		this.urlSafetyValidator = urlSafetyValidator;
	}

	public abstract string SiteName { get; }

	public abstract ScrapersEnum ScrapersEnum { get; }

	protected abstract BaseScraperOptions Options { get; }

	protected abstract string ImageAttribute { get; }

	protected virtual Task OnPreScrapingAsync(IWebDriver driver, CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	protected virtual bool ValidateExternalId(string? externalId)
	{
		return !string.IsNullOrEmpty(externalId);
	}

	/// <summary>
	/// Odvodí externí ID inzerátu z odkazu na detail. Výchozí implementace bere poslední neprázdný
	/// segment cesty bez lomítek, takže na tvaru s koncovým lomítkem ani bez něj nezáleží.
	/// </summary>
	/// <remarks>
	/// POZOR: výsledek se ukládá do databáze jako <c>Listing.ExternalId</c> a slouží k párování
	/// scrapu proti uloženým inzerátům. Každá změna téhle derivace proto vyžaduje ve stejném
	/// nasazení i datovou migraci uložených hodnot - jinak první běh nespáruje nic, všechny
	/// inzeráty se označí za vyřazené a hned založí znovu jako nové, bez cenové historie.
	/// Precedens je migrace NormalizeListingExternalId.
	/// </remarks>
	protected virtual string? ExtractExternalId(Uri detailUri)
	{
		return detailUri.Segments
			.Select(segment => segment.Trim('/'))
			.LastOrDefault(segment => segment.Length > 0);
	}

	protected abstract Task<bool> NavigateToNextPageAsync(
		IWebDriver driver, string baseUrl, int currentPage,
		IReadOnlyList<IWebDriverElement> nextButtons, CancellationToken cancellationToken);

	/// <summary>
	/// Pojistka proti nekonečné stránkovací smyčce (např. trvale přítomné tlačítko "další").
	/// </summary>
	protected const int MaxPages = 100;

	public async Task<ScraperRunResult> ScrapeListingsAsync(ScraperConfiguration scraperConfiguration, CancellationToken cancellationToken)
	{
		var listings = new List<ScraperListingItem>();
		var url = scraperConfiguration.Url;
		var success = true;
		var failedListingsCount = 0;
		var skippedListingsCount = 0;

		// Cílová URL pochází z uživatelského vstupu (cíl scraper úlohy) - před navigací
		// ověříme, že nemíří do interní sítě (SSRF). Při nepovoleném cíli scrape
		// ukončíme jako neúspěšný, aby se nespustila detekce vyřazených inzerátů.
		if (!Uri.TryCreate(url, UriKind.Absolute, out var targetUri)
			|| !await urlSafetyValidator.IsPublicHttpTargetAsync(targetUri, cancellationToken))
		{
			logger.LogError("Cílová URL '{Url}' není platná nebo míří na nepovolený cíl, scrapování se přeskakuje.", url);
			return new ScraperRunResult(false, listings, failedListingsCount, skippedListingsCount);
		}

		IWebDriver? driver = null;
		try
		{
			driver = webDriverFactory.CreateDriver();

			logger.LogTrace("Načítám stránku: {url}", url);
			await driver.NavigateToUrlAsync(url, cancellationToken);

			await Task.Delay(2000, cancellationToken);

			await OnPreScrapingAsync(driver, cancellationToken);

			var load = true;
			int page = 1;
			while (load)
			{
				var listingElements = await driver.FindElementsAsync(Options.ListingSelector, cancellationToken);
				logger.LogInformation("Stránka {Page}: nalezeno {Count} inzerátů", page, listingElements.Count);

				foreach (var element in listingElements)
				{
					try
					{
						var detailElement = await element.FindElementAsync(Options.DetailLinkSelector, cancellationToken);
						var detailUrl = await detailElement.GetAttributeAsync("href", cancellationToken);

						string? externalId = null;
						if (!string.IsNullOrEmpty(detailUrl))
						{
							externalId = ExtractExternalId(new Uri(detailUrl));
						}

						if (!ValidateExternalId(externalId))
						{
							// Karta bez použitelného ID - reklamní blok vsunutý mezi inzeráty nebo developerský
							// projekt. Není to chyba a takový záznam do DB nevstoupí. Počítá se proto, že náhlý
							// nepoměr by znamenal změnu tvaru URL detailu, kdy přestanou procházet i pravé inzeráty.
							skippedListingsCount++;
							logger.LogDebug("Inzerát bez použitelného ID přeskočen, odkaz '{DetailUrl}'.", detailUrl);
							continue;
						}

						var titleElement = await element.FindElementAsync(Options.TitleSelector, cancellationToken);
						var title = await titleElement.GetTextAsync(cancellationToken);

						var priceElement = await element.FindElementAsync(Options.PriceSelector, cancellationToken);
						var priceVal = await priceElement.GetTextAsync(cancellationToken);

						var locationElement = await element.FindElementAsync(Options.LocationSelector, cancellationToken);
						var location = await locationElement.GetTextAsync(cancellationToken);

						var price = PriceParser.ParseNullablePrice(priceVal);

						var imageUrl = string.Empty;
						try
						{
							var imgElement = await element.FindElementAsync(Options.ImageSelector, cancellationToken);
							imageUrl = await imgElement.GetAttributeAsync(ImageAttribute, cancellationToken);
						}
						catch (Exception ex)
						{
							logger.LogDebug(ex, "Nepodařilo se získat obrázek pro inzerát {ExternalId}", externalId);
						}

						var listing = new ScraperListingItem
						{
							Title = title ?? string.Empty,
							Price = price,
							Location = location ?? string.Empty,
							Url = detailUrl ?? string.Empty,
							ImageUrl = imageUrl ?? string.Empty,
							ExternalId = externalId ?? string.Empty
						};

						listings.Add(listing);
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (Exception ex)
					{
						failedListingsCount++;
						logger.LogWarning(ex, "Chyba při zpracování inzerátu");
					}
				}

				page++;

				if (page > MaxPages)
				{
					logger.LogWarning("Dosažen limit {MaxPages} stránek, stránkování se ukončuje.", MaxPages);
					load = false;
					continue;
				}

				var nextButtons = await driver.FindElementsAsync(Options.NextPageSelector, cancellationToken);
				if (nextButtons.Count > 0)
				{
					load = await NavigateToNextPageAsync(driver, url, page, nextButtons, cancellationToken);
				}
				else
				{
					load = false;
				}
			}
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Chyba při scrapování dat z realitního portálu");
			success = false;
		}
		finally
		{
			driver?.Dispose();
		}

		if (skippedListingsCount > 0)
		{
			logger.LogInformation("{SiteName}: {SkippedCount} karet bez použitelného ID přeskočeno, zpracováno {ListingCount}.",
				SiteName, skippedListingsCount, listings.Count);
		}

		return new ScraperRunResult(success, listings, failedListingsCount, skippedListingsCount);
	}
}