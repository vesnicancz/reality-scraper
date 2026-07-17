namespace RealityScraper.Application.Features.Listings;

public class ListingDto
{
	public Guid Id { get; set; }

	public string Title { get; set; } = null!;

	public string Location { get; set; } = null!;

	public decimal? Price { get; set; }

	public string Url { get; set; } = null!;

	public string ImageUrl { get; set; } = null!;

	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset LastSeenAt { get; set; }

	public DateTimeOffset? RemovedAt { get; set; }

	public Guid? ScraperTaskId { get; set; }

	public string? ScraperTaskName { get; set; }
}
