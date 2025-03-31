namespace RealityScraper.Application.Features.Scraping.Model;

public record ScrapingReport
{
	public DateTime ReportDate { get; init; } = DateTime.Now;

	public int NewListingCount => Results.Sum(r => r.NewListingCount);

	public int TotalListingCount => Results.Sum(r => r.TotalListingCount);

	public int PriceChangedListingsCount => Results.Sum(r => r.PriceChangedListings.Count);

	public List<ScraperResult> Results { get; init; } = new List<ScraperResult>();

	public IEnumerable<ScraperResult> GetNotEmptyResults() => Results.Where(r => r.NewListingCount > 0 || r.PriceChangedListingsCount > 0);
}