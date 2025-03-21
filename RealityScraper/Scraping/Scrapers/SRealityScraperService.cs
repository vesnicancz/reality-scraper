using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using RealityScraper.Scraping.Model;

namespace RealityScraper.Scraping.Scrapers;

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

	public async Task<List<ListingItem>> ScrapeListingsAsync()
	{
		var listings = new List<ListingItem>();
		var url = configuration["SRealityScraper:RealityUrl"];
		var searchParameters = configuration.GetSection("SRealityScraper:SearchParameters").Get<Dictionary<string, string>>();

		var rawListings = new List<string>();

		IWebDriver driver = null;
		try
		{
			// Vytvoření URL s parametry
			var urlWithParams = BuildUrlWithParameters(url, searchParameters);

			// Inicializace Selenium driveru
			driver = webDriverFactory.CreateDriver();

			// Nastavení timeoutu
			driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);

			// Načtení stránky
			logger.LogInformation("Načítám stránku: {url}", urlWithParams);
			driver.Navigate().GoToUrl(urlWithParams);

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
							// TODO: PM - revidovat
							rawListings.Add("-- " + innerUrl);
							rawListings.Add("---------------------------");
							continue;
						}

						var title = element.FindElement(By.CssSelector(configuration["SRealityScraper:TitleSelector"])).Text;
						var priceVal = element.FindElement(By.CssSelector(configuration["SRealityScraper:PriceSelector"])).Text;
						var location = element.FindElement(By.CssSelector(configuration["SRealityScraper:LocationSelector"])).Text;

						//decimal.TryParse(priceVal.Replace("Kč", "").Replace(" ", ""), out decimal price);
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

						rawListings.Add(innerUrl + "+" + listingNumber);
						rawListings.Add(title);
						rawListings.Add(priceVal);
						rawListings.Add(location);
						rawListings.Add(imageUrl);
						rawListings.Add("---------------------------");

						var listing = new ListingItem(title, default, price, location, innerUrl, imageUrl, listingNumber);

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
				//var nextButton = driver.FindElements(By.CssSelector("button[data-e2e='show-more-btn']"));
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

		rawListings.Add($"{listings.Count} items found");
		File.WriteAllLines("d:/temp/listings.txt", rawListings);

		return listings;
	}

	private string BuildUrlWithParameters(string baseUrl, Dictionary<string, string> parameters)
	{
		if (parameters == null || !parameters.Any())
		{
			return baseUrl;
		}

		var uriBuilder = new UriBuilder(baseUrl);
		var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);

		foreach (var param in parameters)
		{
			query[param.Key] = param.Value;
		}

		uriBuilder.Query = query.ToString();
		return uriBuilder.ToString();
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