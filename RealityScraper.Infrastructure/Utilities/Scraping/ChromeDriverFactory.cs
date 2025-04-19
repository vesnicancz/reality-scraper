using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Infrastructure.Configuration;

namespace RealityScraper.Infrastructure.Utilities.Scraping;

// Implementace továrny pro Chrome
public class ChromeDriverFactory : IWebDriverFactory
{
	private readonly SeleniumOptions options;
	private readonly ILogger<ChromeDriverFactory> logger;
	private readonly string driverPath;

	public ChromeDriverFactory(
		IOptions<SeleniumOptions> options,
		ILogger<ChromeDriverFactory> logger)
	{
		this.options = options.Value;
		this.logger = logger;

		// Zajistíme, že adresář pro driver existuje
		Directory.CreateDirectory(this.options.DriverPath);
	}

	public RealityScraper.Application.Interfaces.Scraping.IWebDriver CreateDriver()
	{
		try
		{
			var chromeOptions = new ChromeOptions();

			// Přidáme argumenty pro Chrome
			var additionalArgs = options.BrowserArguments;
			if (additionalArgs != null)
			{
				foreach (var arg in additionalArgs)
				{
					chromeOptions.AddArgument(arg);
				}
			}

			// Pokud je konfigurace pro proxy, přidáme ji
			var proxyUrl = options.ProxyUrl;
			if (!string.IsNullOrEmpty(proxyUrl))
			{
				chromeOptions.Proxy = new Proxy
				{
					Kind = ProxyKind.Manual,
					HttpProxy = proxyUrl,
					SslProxy = proxyUrl
				};
			}

			// Nastav user agent pokud je definovaný
			var userAgent = options.UserAgent;
			if (!string.IsNullOrEmpty(userAgent))
			{
				chromeOptions.AddArgument($"--user-agent={userAgent}");
			}

			OpenQA.Selenium.IWebDriver driver;

			// Kontrola zda se má použít remote WebDriver (standalone Selenium)
			var useRemoteDriver = options.UseRemoteDriver;
			if (useRemoteDriver)
			{
				var seleniumHubUrl = options.SeleniumHubUrl;
				logger.LogInformation("Připojuji se k Selenium hub na {seleniumHubUrl}", seleniumHubUrl);
				driver = new RemoteWebDriver(new Uri(seleniumHubUrl), chromeOptions);
			}
			else
			{
				// Vytvoříme servisní objekt pro Chrome
				var service = ChromeDriverService.CreateDefaultService(driverPath);
				service.HideCommandPromptWindow = true;

				// Vytvoříme driver
				driver = new ChromeDriver(service, chromeOptions);
			}

			int timeoutSeconds = options.PageLoadTimeoutSeconds;
			driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(timeoutSeconds);
			return new SeleniumWebDriver(driver);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Chyba při vytváření Chrome driveru");
			throw;
		}
	}
}