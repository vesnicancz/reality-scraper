using OpenQA.Selenium;
using RealityScraper.Application.Interfaces.Scraping;

namespace RealityScraper.Infrastructure.Utilities.Scraping;

public class SeleniumWebElement : IWebDriverElement
{
	private readonly OpenQA.Selenium.IWebDriver webDriver;
	private readonly OpenQA.Selenium.IWebElement seleniumElement;

	public SeleniumWebElement(OpenQA.Selenium.IWebDriver webDriver, OpenQA.Selenium.IWebElement seleniumElement)
	{
		this.webDriver = webDriver;
		this.seleniumElement = seleniumElement;
	}

	public Task<string?> GetAttributeAsync(string attributeName, CancellationToken cancellationToken)
	{
		return Task.FromResult(seleniumElement.GetAttribute(attributeName));
	}

	public Task<string?> GetTextAsync(CancellationToken cancellationToken)
	{
		return Task.FromResult(seleniumElement.Text);
	}

	public Task ClickAsync(CancellationToken cancellationToken)
	{
		seleniumElement.Click();
		return Task.CompletedTask;
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

	public Task<IWebDriverShadowRoot> GetShadowRootAsync(CancellationToken cancellationToken)
	{
		var shadowRoot = seleniumElement.GetShadowRoot();
		var result = new SeleniumShadowRoot(webDriver, shadowRoot);
		return Task.FromResult<IWebDriverShadowRoot>(result);
	}
}