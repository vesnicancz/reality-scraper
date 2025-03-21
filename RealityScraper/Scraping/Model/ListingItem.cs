namespace RealityScraper.Scraping.Model;

public record ListingItem
{
	public string Title { get; init; }

	public decimal? Price { get; init; }

	public string Location { get; init; }

	public string Url { get; init; }

	public string ImageUrl { get; init; }

	public string ExternalId { get; init; }
}