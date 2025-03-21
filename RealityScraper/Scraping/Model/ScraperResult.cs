namespace RealityScraper.Scraping.Model;

public record ScraperResult
{
	public string SiteName { get; init; }

	public int TotalListingCount { get; init; }

	public int NewListingCount => NewListings.Count;

	public int PriceChangedListingsCount => PriceChangedListings.Count;

	public List<ListingItem> NewListings { get; init; } = new List<ListingItem>();

	public List<ListingItemWithNewPrice> PriceChangedListings { get; init; } = new List<ListingItemWithNewPrice>();
}