namespace RealityScraper.Scraping.Model;

public record ListingItem(string Title, string Description, decimal? Price, string Location, string Url, string ImageUrl, string ExternalId);

public record ListingItemWithNewPrice : ListingItem
{
	public decimal? OldPrice { get; init; }

	public ListingItemWithNewPrice(string Title, string Description, decimal? Price, string Location, string Url, string ImageUrl, string ExternalId, decimal? OldPrice) : base(Title, Description, Price, Location, Url, ImageUrl, ExternalId)
	{
		this.OldPrice = OldPrice;
	}
}