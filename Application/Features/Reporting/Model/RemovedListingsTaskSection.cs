namespace RealityScraper.Application.Features.Reporting.Model;

public record RemovedListingsTaskSection
{
	public string ScraperTaskName { get; init; } = string.Empty;

	public List<RemovedListingItem> Listings { get; init; } = new List<RemovedListingItem>();
}
