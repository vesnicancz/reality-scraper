using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using RealityCheck.Data;
using RealityCheck.Model;

namespace RealityCheck.Scraping;

// Služba pro scrapování dat se Selenium
public class RealityScraperService
{
	private readonly ILogger<RealityScraperService> logger;
	private readonly RealityDbContext dbContext;
	private readonly IConfiguration configuration;
	private readonly IWebDriverFactory webDriverFactory;

	public RealityScraperService(
		ILogger<RealityScraperService> logger,
		RealityDbContext dbContext,
		IConfiguration configuration,
		IWebDriverFactory webDriverFactory)
	{
		this.logger = logger;
		this.dbContext = dbContext;
		this.configuration = configuration;
		this.webDriverFactory = webDriverFactory;
	}

	public async Task<List<RealityListing>> ScrapeAndProcessListingsAsync()
	{
		var newListings = new List<RealityListing>();
		var url = configuration["ScraperSettings:RealityUrl"];
		var parameters = configuration.GetSection("ScraperSettings:SearchParameters").Get<Dictionary<string, string>>();

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

			// Čekání na načtení obsahu (může být potřeba upravit dle konkrétního webu)
			var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
			wait.Until(d => d.FindElement(By.CssSelector(configuration["ScraperSettings:ListingSelector"])));

			// Čekání na dokončení JavaScriptu (pokud je potřeba)
			Thread.Sleep(2000);

			// Získání seznamu inzerátů
			var listingElements = driver.FindElements(By.CssSelector(configuration["ScraperSettings:ListingSelector"]));
			logger.LogInformation("Nalezeno {count} inzerátů na stránce.", listingElements.Count);

			foreach (var element in listingElements)
			{
				try
				{
					// Extrahování ID inzerátu - upravit dle konkrétního webu
					var listingId = element.GetAttribute("data-listing-id");

					if (string.IsNullOrEmpty(listingId))
					{
						// Pokud nemá ID, pokusíme se ho získat z URL
						var linkElement = element.FindElement(By.CssSelector("a"));
						var href = linkElement.GetAttribute("href");
						if (!string.IsNullOrEmpty(href))
						{
							var uri = new Uri(href);
							listingId = Path.GetFileName(uri.AbsolutePath);
						}
					}

					if (string.IsNullOrEmpty(listingId))
						continue;

					// Kontrola, zda už tento inzerát máme v databázi
					if (await dbContext.Listings.AnyAsync(l => l.ExternalId == listingId))
						continue;

					// Extrahování dat - upravit selektory dle konkrétního webu
					var title = element.FindElement(By.CssSelector(configuration["ScraperSettings:TitleSelector"])).Text;
					var price = element.FindElement(By.CssSelector(configuration["ScraperSettings:PriceSelector"])).Text;
					var location = element.FindElement(By.CssSelector(configuration["ScraperSettings:LocationSelector"])).Text;
					var innerUrl = element.FindElement(By.CssSelector("a")).GetAttribute("href");

					// Získání URL obrázku
					var imageUrl = "";
					try
					{
						var imgElement = element.FindElement(By.CssSelector(configuration["ScraperSettings:ImageSelector"]));
						imageUrl = imgElement.GetAttribute("src");
						if (string.IsNullOrEmpty(imageUrl))
						{
							// Některé weby používají data-src pro lazy loading
							imageUrl = imgElement.GetAttribute("data-src");
						}
					}
					catch
					{
						// Obrázek není povinný
					}

					var listing = new RealityListing
					{
						ExternalId = listingId,
						Title = title,
						Price = price,
						Location = location,
						Url = innerUrl,
						ImageUrl = imageUrl,
						DiscoveredAt = DateTime.UtcNow
					};

					// Uložení do databáze
					dbContext.Listings.Add(listing);
					newListings.Add(listing);
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Chyba při zpracování inzerátu");

					var screenshot = driver.TakeScreenshot();
					screenshot.SaveAsFile("D:/temp/screenshot.png");
				}
			}

			await dbContext.SaveChangesAsync();
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

		return newListings;
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