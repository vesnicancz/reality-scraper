using OpenQA.Selenium;

namespace RealityScraper.Scraping;

// Rozhraní pro továrnu na WebDriver
public interface IWebDriverFactory
{
	IWebDriver CreateDriver();
}