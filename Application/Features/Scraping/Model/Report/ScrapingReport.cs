namespace RealityScraper.Application.Features.Scraping.Model.Report;

public record ScrapingReport
{
	public DateTimeOffset ReportDate { get; init; }

	public Guid ScraperTaskId { get; init; }

	public string TaskName { get; init; } = string.Empty;

	public List<PortalReport> Results { get; init; } = new List<PortalReport>();

	/// <summary>
	/// True, pokud všechny nakonfigurované scrapery doběhly bez chyby.
	/// Pouze tehdy lze bezpečně detekovat vyřazené inzeráty.
	/// </summary>
	public bool ScrapingSucceeded { get; init; }

	/// <summary>
	/// Inzeráty viděné v tomto běhu (napříč portály), klíčem je externí ID. Nesou čerstvě
	/// nascrapované hodnoty, aby šlo obnovit údaje, které se na portálu během života inzerátu mění.
	/// </summary>
	public IReadOnlyDictionary<string, ScraperListingItem> SeenListings { get; init; } = new Dictionary<string, ScraperListingItem>();

	/// <summary>
	/// Počet inzerátů, které se během scrapování nepodařilo zpracovat (selhaly selektory).
	/// Nenulová hodnota znamená, že SeenListings nemusí být úplné.
	/// </summary>
	public int FailedListingsCount { get; init; }

	/// <summary>
	/// True, pokud některý cíl doběhl úspěšně, ale nevrátil žádný inzerát.
	/// Jeho inzeráty pak nelze bezpečně odlišit od vyřazených.
	/// </summary>
	public bool AnyTargetEmpty { get; init; }

	public int NewListingsCount => Results.Sum(r => r.NewListings.Count);

	public int TotalListingsCount => Results.Sum(r => r.TotalListingsCount);

	public int PriceChangedListingsCount => Results.Sum(r => r.PriceChangedListings.Count);

	public IEnumerable<PortalReport> GetNotEmptyResults() => Results.Where(r => r.NewListingsCount > 0 || r.PriceChangedListingsCount > 0);
}