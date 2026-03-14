namespace RealityScraper.Application.Features.Scraping.Model;

public class ScraperListingItem
{
	public required string Title { get; init; }

	public decimal? Price { get; init; }

	public required string Location { get; init; }

	public required string Url { get; init; }

	public required string ImageUrl { get; init; }

	public required string ExternalId { get; init; }
}