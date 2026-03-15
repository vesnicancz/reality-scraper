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

	protected BaseScraperService(ILogger logger, IWebDriverFactory webDriverFactory)
	{
		this.logger = logger;
		this.webDriverFactory = webDriverFactory;
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

	protected abstract Task<bool> NavigateToNextPageAsync(
		IWebDriver driver, string baseUrl, int currentPage,
		IReadOnlyList<IWebDriverElement> nextButtons, CancellationToken cancellationToken);

	public async Task<List<ScraperListingItem>> ScrapeListingsAsync(ScraperConfiguration scraperConfiguration, CancellationToken cancellationToken)
	{
		var listings = new List<ScraperListingItem>();
		var url = scraperConfiguration.Url;

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
							var uri = new Uri(detailUrl);
							externalId = uri.Segments.Last();
						}

						if (!ValidateExternalId(externalId))
						{
							continue;
						}

						var titleElement = await element.FindElementAsync(Options.TitleSelector, cancellationToken);
						var title = await titleElement.GetTextAsync(cancellationToken);

						var priceElement = await element.FindElementAsync(Options.PriceSelector, cancellationToken);
						var priceVal = await priceElement.GetTextAsync(cancellationToken);

						var locationElement = await element.FindElementAsync(Options.LocationSelector, cancellationToken);
						var location = await locationElement.GetTextAsync(cancellationToken);

						var price = ParseNullableDecimal(priceVal.Replace("Kč", "").Replace(" ", ""));

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
					catch (Exception ex)
					{
						logger.LogWarning(ex, "Chyba při zpracování inzerátu");
					}
				}

				page++;

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
		catch (Exception ex)
		{
			logger.LogError(ex, "Chyba při scrapování dat z realitního portálu");
		}
		finally
		{
			driver?.Dispose();
		}

		return listings;
	}

	protected static decimal? ParseNullableDecimal(string value)
	{
		if (decimal.TryParse(value, out decimal result))
		{
			return result;
		}
		return null;
	}
}