using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RealityScraper.Application.Configuration;
using RealityScraper.Application.Features.Scraping.Configuration;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Application.Features.Scraping.Scrapers;

// Služba pro scrapování dat se Selenium
public class SRealityScraperService : IRealityScraperService
{
	private readonly ILogger<SRealityScraperService> logger;
	private readonly SRealityScraperOptions options;
	private readonly IWebDriverFactory webDriverFactory;

	public SRealityScraperService(
		ILogger<SRealityScraperService> logger,
		IOptions<SRealityScraperOptions> options,
		IWebDriverFactory webDriverFactory)
	{
		this.logger = logger;
		this.options = options.Value;
		this.webDriverFactory = webDriverFactory;
	}

	public string SiteName => "SReality";

	public ScrapersEnum ScrapersEnum => ScrapersEnum.SReality;

	public async Task<List<ListingItem>> ScrapeListingsAsync(ScraperConfiguration scraperConfiguration, CancellationToken cancellationToken)
	{
		var listings = new List<ListingItem>();
		var url = scraperConfiguration.Url;

		IWebDriver? driver = null;
		try
		{
			// Inicializace Selenium driveru
			driver = webDriverFactory.CreateDriver();

			// Načtení stránky
			logger.LogTrace("Načítám stránku: {url}", url);
			await driver.NavigateToUrlAsync(url, cancellationToken);

			// Čekání na dokončení JavaScriptu (pokud je potřeba)
			await Task.Delay(2000);

			// CPM dialog
			var shadowHost = await driver.FindElementsAsync(options.CpmDialogContainerSelector, cancellationToken);
			if (shadowHost.Count > 0)
			{
				// Získání shadow DOM
				var shadowRoot = await shadowHost.First().GetShadowRootAsync(cancellationToken);

				// Najít tlačítko s data-testid="cw-button-agree-with-ads"
				var agreeButtons = await shadowRoot.FindElementsAsync(options.CpmAgreeButtonsSelector, cancellationToken);

				// Kliknout na tlačítko
				await agreeButtons.First().ClickAsync(cancellationToken);

				// Čekání na dokončení JavaScriptu (pokud je potřeba)
				await Task.Delay(5000);
			}

			var load = true;

			while (load)
			{
				// Získání seznamu inzerátů
				var listingElements = await driver.FindElementsAsync(options.ListingSelector, cancellationToken);
				logger.LogInformation("Nalezeno {count} inzerátů na stránce.", listingElements.Count);

				foreach (var element in listingElements)
				{
					try
					{
						// url inzerátu
						string listingNumber = null;
						var detailElement = await element.FindElementAsync(options.DetailLinkSelector, cancellationToken);
						var detailUrl = await detailElement.GetAttributeAsync("href", cancellationToken);

						// id inzerátu
						if (!string.IsNullOrEmpty(detailUrl))
						{
							var uri = new Uri(detailUrl);
							//if (uri.Host.Equals("c.seznam.cz"))
							//{
							//	var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

							//	uri = new Uri(new Uri(query.Get("adurl")).GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Path, UriFormat.Unescaped));
							//}

							listingNumber = uri.Segments.Last();
						}

						if (string.IsNullOrEmpty(listingNumber)
							|| !long.TryParse(listingNumber, out long listingId))
						{
							continue;
						}

						var titleElement = await element.FindElementAsync(options.TitleSelector, cancellationToken);
						var title = await titleElement.GetTextAsync(cancellationToken);

						var priceElement = await element.FindElementAsync(options.PriceSelector, cancellationToken);
						var priceVal = await priceElement.GetTextAsync(cancellationToken);

						var locationElement = await element.FindElementAsync(options.LocationSelector, cancellationToken);
						var location = await locationElement.GetTextAsync(cancellationToken);

						var price = ParseNullableDecimal(priceVal.Replace("Kč", "").Replace(" ", ""));

						var imageUrl = string.Empty;
						try
						{
							var imgElement = await element.FindElementAsync(options.ImageSelector, cancellationToken);
							imageUrl = await imgElement.GetAttributeAsync("src", cancellationToken);
						}
						catch
						{
							// Obrázek není povinný
						}

						var listing = new ListingItem
						{
							Title = title,
							Price = price,
							Location = location,
							Url = detailUrl,
							ImageUrl = imageUrl,
							ExternalId = listingNumber
						};

						listings.Add(listing);
					}
					catch (Exception ex)
					{
						logger.LogWarning(ex, "Chyba při zpracování inzerátu");

						//var screenshot = driver.TakeScreenshot();
						//screenshot.SaveAsFile("D:/temp/screenshot.png");

						//var html = driver.PageSource;
						//File.WriteAllText("D:/temp/page_source.html", html);
					}
				}

				// načtení další strany
				var nextButton = await driver.FindElementsAsync(options.NextPageSelector, cancellationToken);

				if (nextButton.Count > 0)
				{
					await nextButton.First().ClickAsync(cancellationToken);
					await Task.Delay(5000);
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
			// Úklid - zavření prohlížeče
			driver?.Dispose();
		}

		return listings;
	}

	public static decimal? ParseNullableDecimal(string value)
	{
		if (decimal.TryParse(value, out decimal result))
		{
			return result;
		}
		return null;
	}
}