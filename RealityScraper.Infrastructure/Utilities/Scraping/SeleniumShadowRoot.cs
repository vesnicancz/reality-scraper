using OpenQA.Selenium;
using RealityScraper.Application.Interfaces.Scraping;

namespace RealityScraper.Infrastructure.Utilities.Scraping;

public class SeleniumShadowRoot : IWebDriverShadowRoot
{
	private readonly OpenQA.Selenium.IWebDriver webDriver;
	private readonly ISearchContext seleniumElement;

	public SeleniumShadowRoot(OpenQA.Selenium.IWebDriver webDriver, OpenQA.Selenium.ISearchContext seleniumElement)
	{
		this.webDriver = webDriver;
		this.seleniumElement = seleniumElement;
	}

	public Task<IWebDriverElement> FindElementAsync(string selector, CancellationToken cancellationToken)
	{
		var element = seleniumElement.FindElement(By.CssSelector(selector));
		return Task.FromResult<IWebDriverElement>(new SeleniumWebElement(webDriver, element));
	}

	public Task<IReadOnlyList<IWebDriverElement>> FindElementsAsync(string selector, CancellationToken cancellationToken)
	{
		var elements = seleniumElement.FindElements(By.CssSelector(selector))
			.Select(e => new SeleniumWebElement(webDriver, e) as IWebDriverElement)
			.ToList() as IReadOnlyList<IWebDriverElement>;

		return Task.FromResult(elements);
	}
}