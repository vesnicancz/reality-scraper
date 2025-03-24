using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace RealityScraper.Scraping;

// Implementace továrny pro Chrome
public class ChromeDriverFactory : IWebDriverFactory, IDisposable
{
	private readonly IConfiguration configuration;
	private readonly ILogger<ChromeDriverFactory> logger;
	private readonly string driverPath;

	public ChromeDriverFactory(IConfiguration configuration, ILogger<ChromeDriverFactory> logger)
	{
		this.configuration = configuration;
		this.logger = logger;
		this.driverPath = this.configuration.GetValue("SeleniumSettings:DriverPath", "./drivers");

		// Zajistíme, že adresář pro driver existuje
		Directory.CreateDirectory(driverPath);
	}

	public IWebDriver CreateDriver()
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
				return new RemoteWebDriver(new Uri(seleniumHubUrl), options);
			}
			else
			{
				// Vytvoříme servisní objekt pro Chrome
				var service = ChromeDriverService.CreateDefaultService(driverPath);
				service.HideCommandPromptWindow = true;

				// Vytvoříme driver
				return new ChromeDriver(service, options);
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Chyba při vytváření Chrome driveru");
			throw;
		}
	}

	public void Dispose()
	{
		// Případné úklidy
	}
}