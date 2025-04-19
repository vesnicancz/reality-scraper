using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scraping.Configuration;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Features.Scraping.Scrapers;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Infrastructure.Utilities.Scraping;

public class RealityIdnesScraperService : IRealityScraperService
{
	private readonly ILogger<RealityIdnesScraperService> logger;
	private readonly IConfiguration configuration;
	private readonly IWebDriverFactory webDriverFactory;

	public RealityIdnesScraperService(
		ILogger<RealityIdnesScraperService> logger,
		IConfiguration configuration,
		IWebDriverFactory webDriverFactory)
	{
		this.logger = logger;
		this.configuration = configuration;
		this.webDriverFactory = webDriverFactory;
	}

	public string SiteName => "Reality Idnes";

	public ScrapersEnum ScrapersEnum => ScrapersEnum.RealityIdnes;

	public async Task<List<ListingItem>> ScrapeListingsAsync(ScraperConfiguration scraperConfiguration, CancellationToken cancellationToken)
	{
		var listings = new List<ListingItem>();
		var url = scraperConfiguration.Url;

		IWebDriver driver = null;

		try
		{
			// Inicializace Selenium driveru
			driver = webDriverFactory.CreateDriver();

			// Nastavení timeoutu
			//driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);

			// Načtení stránky
			logger.LogInformation("Načítám stránku: {url}", url);
			//driver.Navigate().GoToUrl(url);
			await driver.NavigateToUrlAsync(url, cancellationToken);

			// Čekání na dokončení JavaScriptu (pokud je potřeba)
			await Task.Delay(2000);

			var load = true;

			while (load)
			{
				// Získání seznamu inzerátů
				//var listingElements = driver.FindElements(By.CssSelector(configuration["RealityIdnesScraper:ListingSelector"]));
				var listingElements = await driver.FindElementsAsync(configuration["RealityIdnesScraper:ListingSelector"], cancellationToken);
				logger.LogInformation("Nalezeno {count} inzerátů na stránce.", listingElements.Count);

				foreach (var element in listingElements)
				{
					try
					{
						// url inzerátu
						//var linkElement = element.FindElement(By.CssSelector("a"));
						var linkElement = await element.FindElementAsync("a", cancellationToken);
						var innerUrl = await linkElement.GetAttributeAsync("href", cancellationToken);

						// id inzerátu
						string listingNumber = null;
						if (!string.IsNullOrEmpty(innerUrl))
						{
							var uri = new Uri(innerUrl);
							listingNumber = uri.Segments.Last();
						}

						//var title = element.FindElement(By.CssSelector(configuration["RealityIdnesScraper:TitleSelector"])).Text;
						var titleElement = (await element.FindElementAsync(configuration["RealityIdnesScraper:TitleSelector"], cancellationToken));
						var title = await titleElement.GetTextAsync(cancellationToken);

						//var priceVal = element.FindElement(By.CssSelector(configuration["RealityIdnesScraper:PriceSelector"])).Text;
						var priceElement = await element.FindElementAsync(configuration["RealityIdnesScraper:PriceSelector"], cancellationToken);
						var priceVal = await priceElement.GetTextAsync(cancellationToken);

						//var location = element.FindElement(By.CssSelector(configuration["RealityIdnesScraper:LocationSelector"])).Text;
						var locationElement = await element.FindElementAsync(configuration["RealityIdnesScraper:LocationSelector"], cancellationToken);
						var location = await locationElement.GetTextAsync(cancellationToken);

						var price = ParseNullableDecimal(priceVal.Replace("Kč", "").Replace(" ", ""));

						var imageUrl = string.Empty;
						try
						{
							//var imgElement = element.FindElement(By.CssSelector(configuration["RealityIdnesScraper:ImageSelector"]));
							var imgElement = await element.FindElementAsync(configuration["RealityIdnesScraper:ImageSelector"], cancellationToken);
							imageUrl = await imgElement.GetAttributeAsync("data-src", cancellationToken);
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
							Url = innerUrl,
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
				//var nextButton = driver.FindElements(By.CssSelector(configuration["RealityIdnesScraper:NextPageSelector"]));
				var nextButton = await driver.FindElementsAsync(configuration["RealityIdnesScraper:NextPageSelector"], cancellationToken);

				if (nextButton.Count > 0)
				{
					var nextPageElement = nextButton.First();
					var nextPageLink = await nextPageElement.GetAttributeAsync("href", cancellationToken);
					//driver.Navigate().GoToUrl(nextPageLink);
					await driver.NavigateToUrlAsync(nextPageLink, cancellationToken);
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