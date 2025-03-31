using OpenQA.Selenium;

namespace RealityScraper.Application.Features.Scraping;

// Rozhraní pro továrnu na WebDriver
public interface IWebDriverFactory
{
	IWebDriver CreateDriver();
}