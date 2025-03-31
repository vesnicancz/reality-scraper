using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using RealityScraper.Application.Features.Scheduling.Configuration;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Interfaces.Scraping;

namespace RealityScraper.Application.Features.Scraping.Scrapers;

// Služba pro scrapování dat se Selenium
public class SRealityScraperService : IRealityScraperService
{
	private readonly ILogger<SRealityScraperService> logger;
	private readonly IConfiguration configuration;
	private readonly IWebDriverFactory webDriverFactory;

	public SRealityScraperService(
		ILogger<SRealityScraperService> logger,
		IConfiguration configuration,
		IWebDriverFactory webDriverFactory)
	{
		this.logger = logger;
		this.configuration = configuration;
		this.webDriverFactory = webDriverFactory;
	}

	public string SiteName => "SReality";

	public ScrapersEnum ScrapersEnum => ScrapersEnum.SReality;

	public async Task<List<ListingItem>> ScrapeListingsAsync(ScraperConfiguration scraperConfiguration)
	{
		var listings = new List<ListingItem>();
		var url = scraperConfiguration.Url;

		IWebDriver? driver = null;
		try
		{
			// Inicializace Selenium driveru
			driver = webDriverFactory.CreateDriver();

			// Nastavení timeoutu
			driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);

			// Načtení stránky
			logger.LogInformation("Načítám stránku: {url}", url);
			driver.Navigate().GoToUrl(url);

			// Čekání na dokončení JavaScriptu (pokud je potřeba)
			await Task.Delay(2000);

			// Příklad přístupu k shadow DOM elementu
			var shadowHost = driver.FindElements(By.CssSelector(".szn-cmp-dialog-container"));
			if (shadowHost.Count > 0)
			{
				var shadowRoot = shadowHost.First().GetShadowRoot();

				// Najít tlačítko s data-testid="cw-button-agree-with-ads"
				var agreeButtons = shadowRoot.FindElements(By.CssSelector("button[data-testid='cw-button-agree-with-ads']"));

				// Kliknout na tlačítko
				agreeButtons.First().Click();

				// Čekání na dokončení JavaScriptu (pokud je potřeba)
				await Task.Delay(5000);
			}

			var load = true;

			while (load)
			{
				// Získání seznamu inzerátů
				var listingElements = driver.FindElements(By.CssSelector(configuration["SRealityScraper:ListingSelector"]));
				logger.LogInformation("Nalezeno {count} inzerátů na stránce.", listingElements.Count);

				foreach (var element in listingElements)
				{
					try
					{
						// url inzerátu
						string listingNumber = null;
						var linkElement = element.FindElement(By.CssSelector("a"));
						var innerUrl = linkElement.GetAttribute("href");

						// id inzerátu
						if (!string.IsNullOrEmpty(innerUrl))
						{
							var uri = new Uri(innerUrl);
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

						var title = element.FindElement(By.CssSelector(configuration["SRealityScraper:TitleSelector"])).Text;
						var priceVal = element.FindElement(By.CssSelector(configuration["SRealityScraper:PriceSelector"])).Text;
						var location = element.FindElement(By.CssSelector(configuration["SRealityScraper:LocationSelector"])).Text;

						var price = ParseNullableDecimal(priceVal.Replace("Kč", "").Replace(" ", ""));

						var imageUrl = string.Empty;
						try
						{
							var imgElement = element.FindElement(By.CssSelector(configuration["SRealityScraper:ImageSelector"]));
							imageUrl = imgElement.GetAttribute("src");
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
				var nextButton = driver.FindElements(By.CssSelector(configuration["SRealityScraper:NextPageSelector"]));
				if (nextButton.Count > 0)
				{
					nextButton.First().Click();
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
			driver?.Quit();
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