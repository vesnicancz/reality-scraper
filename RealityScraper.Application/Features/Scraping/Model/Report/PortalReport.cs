namespace RealityScraper.Application.Features.Scraping.Model.Report;

public record PortalReport
{
	public string SiteName { get; init; }

	public int TotalListingsCount { get; init; }

	public int NewListingsCount => NewListings.Count;

	public int PriceChangedListingsCount => PriceChangedListings.Count;

	public List<ListingItem> NewListings { get; init; } = new List<ListingItem>();

	public List<ListingItemWithNewPrice> PriceChangedListings { get; init; } = new List<ListingItemWithNewPrice>();
}