namespace RealityScraper.Application.Interfaces.Scraping;

public interface IWebDriverShadowRoot
{
	/// <summary>
	/// Najde první dceřiný element odpovídající selektoru.
	/// </summary>
	Task<IWebDriverElement> FindElementAsync(string selector, CancellationToken cancellationToken);

	/// <summary>
	/// Najde všechny dceřiné elementy odpovídající selektoru.
	/// </summary>
	Task<IReadOnlyList<IWebDriverElement>> FindElementsAsync(string selector, CancellationToken cancellationToken);
}