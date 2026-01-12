namespace RealityScraper.Application.Features.Scraping.Model.Report;

public record ScrapingReport
{
	public DateTime ReportDate { get; init; } = DateTime.Now;

	public Guid ScraperTaskId { get; init; }

	public List<PortalReport> Results { get; init; } = new List<PortalReport>();

	public int NewListingsCount => Results.Sum(r => r.NewListings.Count);

	public int TotalListingsCount => Results.Sum(r => r.TotalListingsCount);

	public int PriceChangedListingsCount => Results.Sum(r => r.PriceChangedListings.Count);

	public IEnumerable<PortalReport> GetNotEmptyResults() => Results.Where(r => r.NewListingsCount > 0 || r.PriceChangedListingsCount > 0);
}