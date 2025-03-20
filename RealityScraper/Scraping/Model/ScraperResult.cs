namespace RealityScraper.Scraping.Model;

public record ScraperResult(string SiteName, int TotalListingCount)
{
	public int NewListingCount => NewListings.Count;

	public List<ListingItem> NewListings { get; set; } = new List<ListingItem>();
}