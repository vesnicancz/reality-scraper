namespace RealityScraper.Application.Interfaces.Scraping;

public interface IWebDriver : IDisposable
{
	/// <summary>
	/// Přejde na zadanou URL
	/// </summary>
	Task NavigateToUrlAsync(string url, CancellationToken cancellationToken);

	/// <summary>
	/// Najde první element odpovídající selektoru
	/// </summary>
	Task<IWebDriverElement?> FindElementAsync(string cssSelector, CancellationToken cancellationToken);

	/// <summary>
	/// Najde všechny elementy odpovídající selektoru
	/// </summary>
	Task<IReadOnlyList<IWebDriverElement>> FindElementsAsync(string cssSelector, CancellationToken cancellationToken);

	/// <summary>
	/// Získá screenshot aktuální stránky jako pole bajtů
	/// </summary>
	Task<byte[]> TakeScreenshotAsync();

	/// <summary>
	/// Získá zdrojový kód stránky
	/// </summary>
	Task<string> GetPageSourceAsync();
}