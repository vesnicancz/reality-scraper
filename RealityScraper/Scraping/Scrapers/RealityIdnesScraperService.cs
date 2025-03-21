using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using RealityScraper.Scraping.Model;

namespace RealityScraper.Scraping.Scrapers;

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

	public async Task<List<ListingItem>> ScrapeListingsAsync()
	{
		var listings = new List<ListingItem>();
		var url = configuration["RealityIdnesScraper:RealityUrl"];

		var rawListings = new List<string>();

		IWebDriver driver = null;

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

			var load = true;

			while (load)
			{
				// Získání seznamu inzerátů
				var listingElements = driver.FindElements(By.CssSelector(configuration["RealityIdnesScraper:ListingSelector"]));
				//var listingElements = driver.FindElements(By.CssSelector("div.c-products div.c-products__item div:not(.r-tipserveru) article"));
				logger.LogInformation("Nalezeno {count} inzerátů na stránce.", listingElements.Count);

				foreach (var element in listingElements)
				{
					try
					{
						// url inzerátu
						var linkElement = element.FindElement(By.CssSelector("a"));
						var innerUrl = linkElement.GetAttribute("href");

						// id inzerátu
						string listingNumber = null;
						if (!string.IsNullOrEmpty(innerUrl))
						{
							var uri = new Uri(innerUrl);
							listingNumber = uri.Segments.Last();
						}

						var title = element.FindElement(By.CssSelector(configuration["RealityIdnesScraper:TitleSelector"])).Text;
						//var title = element.FindElement(By.CssSelector("h2.c-products__title")).Text;
						var priceVal = element.FindElement(By.CssSelector(configuration["RealityIdnesScraper:PriceSelector"])).Text;
						//var priceVal = element.FindElement(By.CssSelector("p.c-products__price")).Text;
						var location = element.FindElement(By.CssSelector(configuration["RealityIdnesScraper:LocationSelector"])).Text;
						//var location = element.FindElement(By.CssSelector("p.c-products__info")).Text;

						//decimal.TryParse(priceVal.Replace("Kč", "").Replace(" ", ""), out decimal price);
						var price = ParseNullableDecimal(priceVal.Replace("Kč", "").Replace(" ", ""));

						var imageUrl = string.Empty;
						try
						{
							var imgElement = element.FindElement(By.CssSelector(configuration["RealityIdnesScraper:ImageSelector"]));
							//var imgElement = element.FindElement(By.CssSelector("img"));
							imageUrl = imgElement.GetAttribute("data-src");
						}
						catch
						{
							// Obrázek není povinný
						}

						rawListings.Add(innerUrl + "+" + listingNumber);
						rawListings.Add(title);
						rawListings.Add(priceVal);
						rawListings.Add(location);
						rawListings.Add(imageUrl);
						rawListings.Add("---------------------------");

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

						var screenshot = driver.TakeScreenshot();
						screenshot.SaveAsFile("D:/temp/screenshot.png");

						var html = driver.PageSource;
						File.WriteAllText("D:/temp/page_source.html", html);
					}
				}

				// načtení další strany
				//btn paging__item next
				//var nextButton = driver.FindElements(By.CssSelector(".btn.paging__item.next"));
				var nextButton = driver.FindElements(By.CssSelector(configuration["RealityIdnesScraper:NextPageSelector"]));

				if (nextButton.Count > 0)
				{
					//logger.LogInformation("Načítám další stránku...");
					var nextPageLink = nextButton.First().GetAttribute("href");
					driver.Navigate().GoToUrl(nextPageLink);
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

		rawListings.Add($"{listings.Count} items found");
		File.WriteAllLines("d:/temp/listings2.txt", rawListings);

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