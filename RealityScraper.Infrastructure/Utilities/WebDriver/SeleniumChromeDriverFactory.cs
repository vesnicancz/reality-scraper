using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Infrastructure.Configuration;

namespace RealityScraper.Infrastructure.Utilities.WebDriver;

/// <summary>
/// Selenium Chrome WebDriver factory.
/// </summary>
public class SeleniumChromeDriverFactory : IWebDriverFactory
{
	private readonly SeleniumOptions options;
	private readonly ILogger<SeleniumChromeDriverFactory> logger;

	public SeleniumChromeDriverFactory(
		IOptions<SeleniumOptions> options,
		ILogger<SeleniumChromeDriverFactory> logger)
	{
		this.options = options.Value;
		this.logger = logger;
	}

	public Application.Interfaces.Scraping.IWebDriver CreateDriver()
	{
		try
		{
			var chromeOptions = new ChromeOptions();

			// Add arguments for Chrome
			var additionalArgs = options.BrowserArguments;
			if (additionalArgs != null)
			{
				foreach (var arg in additionalArgs)
				{
					chromeOptions.AddArgument(arg);
				}
			}

			// If there is a configuration for a proxy, add it
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

			// Set user agent if defined
			var userAgent = options.UserAgent;
			if (!string.IsNullOrEmpty(userAgent))
			{
				chromeOptions.AddArgument($"--user-agent={userAgent}");
			}

			OpenQA.Selenium.IWebDriver driver;

			// Check if a remote WebDriver (standalone Selenium) should be used
			var useRemoteDriver = options.UseRemoteDriver;
			if (useRemoteDriver)
			{
				var seleniumHubUrl = options.SeleniumHubUrl;
				logger.LogInformation("Připojuji se k Selenium hub na {seleniumHubUrl}", seleniumHubUrl);
				driver = new RemoteWebDriver(new Uri(seleniumHubUrl), chromeOptions);
			}
			else
			{
				// Ensure driver path exists
				Directory.CreateDirectory(options.DriverPath);

				// Create a service object for Chrome
				var service = ChromeDriverService.CreateDefaultService(options.DriverPath);
				service.HideCommandPromptWindow = true;

				// Create the driver
				logger.LogInformation("Vytvářím Chrome driver na {driverPath}", options.DriverPath);
				driver = new ChromeDriver(service, chromeOptions);
			}

			// Set the timeout for page loading
			driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(options.PageLoadTimeoutSeconds);

			return new SeleniumWebDriver(driver);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Chyba při vytváření Chrome driveru");
			throw;
		}
	}
}