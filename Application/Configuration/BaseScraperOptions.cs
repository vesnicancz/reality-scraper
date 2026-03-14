namespace RealityScraper.Application.Configuration;

public class BaseScraperOptions
{
	public required string ListingSelector { get; set; }

	public required string DetailLinkSelector { get; set; }

	public required string TitleSelector { get; set; }

	public required string PriceSelector { get; set; }

	public required string LocationSelector { get; set; }

	public required string ImageSelector { get; set; }

	public required string NextPageSelector { get; set; }
}