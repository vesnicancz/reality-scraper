namespace RealityScraper.Scraping.Model;

public record ScraperResult(string SiteName, int TotalListingCount)
{
	public int NewListingCount => NewListings.Count;

	public int PriceChangedListingsCount => PriceChangedListings.Count;

	public List<ListingItem> NewListings { get; set; } = new List<ListingItem>();

	public List<ListingItemWithNewPrice> PriceChangedListings { get; set; } = new List<ListingItemWithNewPrice>();
}