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
	/// Externí ID všech inzerátů viděných v tomto běhu (napříč portály).
	/// </summary>
	public IReadOnlySet<string> SeenExternalIds { get; init; } = new HashSet<string>();

	public int NewListingsCount => Results.Sum(r => r.NewListings.Count);

	public int TotalListingsCount => Results.Sum(r => r.TotalListingsCount);

	public int PriceChangedListingsCount => Results.Sum(r => r.PriceChangedListings.Count);

	public IEnumerable<PortalReport> GetNotEmptyResults() => Results.Where(r => r.NewListingsCount > 0 || r.PriceChangedListingsCount > 0);
}