namespace RealityScraper.Scraping.Model;

public record ListingItemWithNewPrice : ListingItem
{
	public decimal? OldPrice { get; init; }

	public decimal? PriceDiff => Price - OldPrice;
}