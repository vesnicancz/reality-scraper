namespace RealityScraper.Scraping.Model;

public record ScrapingReport
{
	public int NewListingCount => Results.Sum(r => r.NewListingCount);

	public int TotalListingCount => Results.Sum(r => r.TotalListingCount);

	public List<ScraperResult> Results { get; set; } = new List<ScraperResult>();
}