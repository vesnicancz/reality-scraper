using OpenQA.Selenium;

namespace RealityScraper.Infrastructure.Utilities.Scraping;

// Rozhraní pro továrnu na WebDriver
public interface IWebDriverFactory
{
	IWebDriver CreateDriver();
}