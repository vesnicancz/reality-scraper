using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using RealityScraper.Application.Interfaces.Scraping;

namespace RealityScraper.Infrastructure.Utilities.Scraping;

// Implementace továrny pro Chrome
public class ChromeDriverFactory : IWebDriverFactory
{
	private readonly IConfiguration configuration;
	private readonly ILogger<ChromeDriverFactory> logger;
	private readonly string driverPath;

	public ChromeDriverFactory(IConfiguration configuration, ILogger<ChromeDriverFactory> logger)
	{
		this.configuration = configuration;
		this.logger = logger;
		driverPath = this.configuration.GetValue("SeleniumSettings:DriverPath", "./drivers");

		// Zajistíme, že adresář pro driver existuje
		Directory.CreateDirectory(driverPath);
	}

	public RealityScraper.Application.Interfaces.Scraping.IWebDriver CreateDriver()
	{
		try
		{
			var options = new ChromeOptions();

			// Přidáme argumenty pro Chrome
			var additionalArgs = configuration.GetSection("SeleniumSettings:BrowserArguments").Get<List<string>>();
			if (additionalArgs != null)
			{
				foreach (var arg in additionalArgs)
				{
					options.AddArgument(arg);
				}
			}

			// Pokud je konfigurace pro proxy, přidáme ji
			var proxyUrl = configuration.GetValue<string>("SeleniumSettings:ProxyUrl");
			if (!string.IsNullOrEmpty(proxyUrl))
			{
				options.Proxy = new Proxy
				{
					Kind = ProxyKind.Manual,
					HttpProxy = proxyUrl,
					SslProxy = proxyUrl
				};
			}

			// Nastav user agent pokud je definovaný
			var userAgent = configuration.GetValue<string>("SeleniumSettings:UserAgent");
			if (!string.IsNullOrEmpty(userAgent))
			{
				options.AddArgument($"--user-agent={userAgent}");
			}

			OpenQA.Selenium.IWebDriver driver;

			// Kontrola zda se má použít remote WebDriver (standalone Selenium)
			var useRemoteDriver = configuration.GetValue<bool>("SeleniumSettings:UseRemoteDriver");
			if (useRemoteDriver)
			{
				var seleniumHubUrl = configuration.GetValue<string>("SeleniumSettings:SeleniumHubUrl");
				if (string.IsNullOrEmpty(seleniumHubUrl))
				{
					seleniumHubUrl = "http://localhost:4444/wd/hub"; // Výchozí URL pro Selenium v Docker compose
				}

				logger.LogInformation("Připojuji se k Selenium hub na {seleniumHubUrl}", seleniumHubUrl);
				driver = new RemoteWebDriver(new Uri(seleniumHubUrl), options);
			}
			else
			{
				// Vytvoříme servisní objekt pro Chrome
				var service = ChromeDriverService.CreateDefaultService(driverPath);
				service.HideCommandPromptWindow = true;

				// Vytvoříme driver
				driver = new ChromeDriver(service, options);
			}

			int timeoutSeconds = configuration.GetValue<int>("SeleniumSettings:PageLoadTimeoutSeconds", 30);
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