using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using RealityScraper.Model;

namespace RealityScraper.Scraping;

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

	public async Task<List<Listing>> ScrapeListingsAsync()
	{
		var listings = new List<Listing>();
		var url = configuration["ScraperSettings:RealityUrl"];
		var parameters = configuration.GetSection("ScraperSettings:SearchParameters").Get<Dictionary<string, string>>();

		var rawListings = new List<string>();

		IWebDriver driver = null;
		try
		{
			// Vytvoření URL s parametry
			var urlWithParams = BuildUrlWithParameters(url, parameters);

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
				//var shadowRoot = (ShadowRoot)((IJavaScriptExecutor)driver).ExecuteScript("return arguments[0].shadowRoot", shadowHost.First());
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
				var listingElements = driver.FindElements(By.CssSelector(configuration["ScraperSettings:ListingSelector"]));
				//var listingElements = driver.FindElements(By.CssSelector("ul.MuiGrid2-direction-xs-row[data-e2e='estates-list']>li"));
				logger.LogInformation("Nalezeno {count} inzerátů na stránce.", listingElements.Count);

				foreach (var element in listingElements)
				{
					try
					{
						// Extrahování ID inzerátu
						string listingNumber = null;
						var linkElement = element.FindElement(By.CssSelector("a")); // tady to padá
						var innerUrl = linkElement.GetAttribute("href");
						if (!string.IsNullOrEmpty(innerUrl))
						{
							var uri = new Uri(innerUrl);
							listingNumber = Path.GetFileName(uri.AbsolutePath);
						}

						if (string.IsNullOrEmpty(listingNumber)
							|| !long.TryParse(listingNumber, out long listingId))
						{
							rawListings.Add("-- " + innerUrl);
							rawListings.Add("---------------------------");
							continue;
						}

						var title = element.FindElement(By.CssSelector(configuration["ScraperSettings:TitleSelector"])).Text;
						//var title = element.FindElement(By.CssSelector("p:nth-of-type(1)")).Text;
						var priceVal = element.FindElement(By.CssSelector(configuration["ScraperSettings:PriceSelector"])).Text;
						//var price = element.FindElement(By.CssSelector("p:nth-of-type(3)")).Text;
						var location = element.FindElement(By.CssSelector(configuration["ScraperSettings:LocationSelector"])).Text;
						//var location = element.FindElement(By.CssSelector("p:nth-of-type(2)")).Text;

						decimal.TryParse(priceVal.Replace("Kč", "").Replace(" ", ""), out decimal price);

						var imageUrl = string.Empty;
						try
						{
							var imgElement = element.FindElement(By.CssSelector(configuration["ScraperSettings:ImageSelector"]));
							//var imgElement = element.FindElement(By.CssSelector("ul li:nth-of-type(1) img:nth-of-type(2)"));
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

						// Extrahování dat - upravit selektory dle konkrétního webu
						//var innerUrl = element.FindElement(By.CssSelector("a")).GetAttribute("href");

						//// Získání URL obrázku
						//var imageUrl = "";
						//try
						//{
						//	var imgElement = element.FindElement(By.CssSelector(configuration["ScraperSettings:ImageSelector"]));
						//	imageUrl = imgElement.GetAttribute("src");
						//	if (string.IsNullOrEmpty(imageUrl))
						//	{
						//		// Některé weby používají data-src pro lazy loading
						//		imageUrl = imgElement.GetAttribute("data-src");
						//	}
						//}
						//catch
						//{
						//	// Obrázek není povinný
						//}

						var listing = new Listing
						{
							ExternalId = listingId,
							Title = title,
							Price = price,
							Location = location,
							Url = innerUrl,
							ImageUrl = imageUrl,
							DiscoveredAt = DateTime.UtcNow
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

				// tlačítko pro další stránku
				var nextButton = driver.FindElements(By.CssSelector("button[data-e2e='show-more-btn']"));
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
}