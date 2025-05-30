﻿using OpenQA.Selenium;
using RealityScraper.Application.Interfaces.Scraping;

namespace RealityScraper.Infrastructure.Utilities.WebDriver;

public class SeleniumWebDriver : Application.Interfaces.Scraping.IWebDriver
{
	private bool disposed;

	private readonly OpenQA.Selenium.IWebDriver driver;

	public SeleniumWebDriver(OpenQA.Selenium.IWebDriver driver)
	{
		this.driver = driver;
	}

	public Task NavigateToUrlAsync(string url, CancellationToken cancellationToken)
	{
		//await driver.Navigate().GoToUrlAsync(url);
		driver.Navigate().GoToUrl(url);
		return Task.CompletedTask;
	}

	public Task<IWebDriverElement?> FindElementAsync(string selector, CancellationToken cancellationToken)
	{
		var element = driver.FindElement(By.CssSelector(selector));
		return Task.FromResult<IWebDriverElement?>(new SeleniumWebElement(driver, element));
	}

	public Task<IReadOnlyList<IWebDriverElement>> FindElementsAsync(string selector, CancellationToken cancellationToken)
	{
		var elements = driver.FindElements(By.CssSelector(selector))
			.Select(e => new SeleniumWebElement(driver, e) as IWebDriverElement)
			.ToList() as IReadOnlyList<IWebDriverElement>;

		return Task.FromResult(elements);
	}

	public Task<byte[]> TakeScreenshotAsync()
	{
		var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
		return Task.FromResult(screenshot.AsByteArray);
	}

	public Task<string> GetPageSourceAsync()
	{
		return Task.FromResult(driver.PageSource);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				driver?.Quit();
				driver?.Dispose();
			}
			disposed = true;
		}
	}
}