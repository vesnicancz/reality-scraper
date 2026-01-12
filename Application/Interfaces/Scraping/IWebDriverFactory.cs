namespace RealityScraper.Application.Interfaces.Scraping;

// Rozhraní pro továrnu na WebDriver
public interface IWebDriverFactory
{
	IWebDriver CreateDriver();
}