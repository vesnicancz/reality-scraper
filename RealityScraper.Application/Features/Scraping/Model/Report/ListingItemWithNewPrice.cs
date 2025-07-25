namespace RealityScraper.Application.Features.Scraping.Model.Report;

public record ListingItemWithNewPrice : ListingItem
{
	public decimal? OldPrice { get; init; }

	public decimal? PriceDiff => Price - OldPrice;
}