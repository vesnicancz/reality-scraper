namespace RealityScraper.Application.Interfaces.Scraping;

public interface IWebDriverElement
{
	/// <summary>
	/// Získá hodnotu atributu elementu.
	/// </summary>
	Task<string?> GetAttributeAsync(string attributeName, CancellationToken cancellationToken);

	/// <summary>
	/// Získá textový obsah elementu.
	/// </summary>
	Task<string?> GetTextAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Klikne na element
	/// </summary>
	Task ClickAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Najde první dceřiný element odpovídající selektoru.
	/// </summary>
	Task<IWebDriverElement> FindElementAsync(string selector, CancellationToken cancellationToken);

	/// <summary>
	/// Najde všechny dceřiné elementy odpovídající selektoru.
	/// </summary>
	Task<IReadOnlyList<IWebDriverElement>> FindElementsAsync(string selector, CancellationToken cancellationToken);

	Task<IWebDriverShadowRoot> GetShadowRootAsync(CancellationToken cancellationToken);
}