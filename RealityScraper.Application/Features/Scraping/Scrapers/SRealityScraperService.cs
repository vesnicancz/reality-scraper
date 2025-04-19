using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scraping.Configuration;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Features.Scraping.Scrapers;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Infrastructure.Utilities.Scraping;

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

	public async Task<List<ListingItem>> ScrapeListingsAsync(ScraperConfiguration scraperConfiguration, CancellationToken cancellationToken)
	{
		var listings = new List<ListingItem>();
		var url = scraperConfiguration.Url;

		//OpenQA.Selenium.IWebDriver? driver = null;
		IWebDriver? driver = null;
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

			// Příklad přístupu k shadow DOM elementu
			//var shadowHost = driver.FindElements(By.CssSelector(".szn-cmp-dialog-container"));
			var shadowHost = await driver.FindElementsAsync(".szn-cmp-dialog-container", cancellationToken);

			if (shadowHost.Count > 0)
			{
				var shadowRoot = await shadowHost.First().GetShadowRootAsync(cancellationToken);

				// Najít tlačítko s data-testid="cw-button-agree-with-ads"
				//var agreeButtons = shadowRoot.FindElements(By.CssSelector("button[data-testid='cw-button-agree-with-ads']"));
				var agreeButtons = await shadowRoot.FindElementsAsync("button[data-testid='cw-button-agree-with-ads']", cancellationToken);

				// Kliknout na tlačítko
				await agreeButtons.First().ClickAsync(cancellationToken);

				// Čekání na dokončení JavaScriptu (pokud je potřeba)
				await Task.Delay(5000);
			}

			var load = true;

			while (load)
			{
				// Získání seznamu inzerátů
				//var listingElements = driver.FindElements(By.CssSelector(configuration["SRealityScraper:ListingSelector"]));
				var listingElements = await driver.FindElementsAsync(configuration["SRealityScraper:ListingSelector"], cancellationToken);
				logger.LogInformation("Nalezeno {count} inzerátů na stránce.", listingElements.Count);

				foreach (var element in listingElements)
				{
					try
					{
						// url inzerátu
						string listingNumber = null;
						//var linkElement = element.FindElement(By.CssSelector("a"));
						var linkElement = await element.FindElementAsync("a", cancellationToken);

						var innerUrl = await linkElement.GetAttributeAsync("href", cancellationToken);

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

						//var title = element.FindElement(By.CssSelector(configuration["SRealityScraper:TitleSelector"])).Text;
						var titleElement = await element.FindElementAsync(configuration["SRealityScraper:TitleSelector"], cancellationToken);
						var title = await titleElement.GetTextAsync(cancellationToken);

						//var priceVal = element.FindElement(By.CssSelector(configuration["SRealityScraper:PriceSelector"])).Text;
						var priceElement = await element.FindElementAsync(configuration["SRealityScraper:PriceSelector"], cancellationToken);
						var priceVal = await priceElement.GetTextAsync(cancellationToken);

						//var location = element.FindElement(By.CssSelector(configuration["SRealityScraper:LocationSelector"])).Text;
						var locationElement = await element.FindElementAsync(configuration["SRealityScraper:LocationSelector"], cancellationToken);
						var location = await locationElement.GetTextAsync(cancellationToken);

						var price = ParseNullableDecimal(priceVal.Replace("Kč", "").Replace(" ", ""));

						var imageUrl = string.Empty;
						try
						{
							//var imgElement = element.FindElement(By.CssSelector(configuration["SRealityScraper:ImageSelector"]));
							var imgElement = await element.FindElementAsync(configuration["SRealityScraper:ImageSelector"], cancellationToken);

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
				//var nextButton = driver.FindElements(By.CssSelector(configuration["SRealityScraper:NextPageSelector"]));
				var nextButton = await driver.FindElementsAsync(configuration["SRealityScraper:NextPageSelector"], cancellationToken);

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