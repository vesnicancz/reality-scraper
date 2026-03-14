using RealityScraper.SharedKernel;

namespace RealityScraper.Domain.Entities.Realty;

public class Listing : Entity
{
	public string ExternalId { get; set; } = null!;

	public string Title { get; set; } = null!;

	public string Location { get; set; } = null!;

	public decimal? Price { get; set; }

	public string Url { get; set; } = null!;

	public string ImageUrl { get; set; } = null!;

	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset LastSeenAt { get; set; }

	public DateTimeOffset PriceFrom { get; set; }

	public Guid? ScraperTaskId { get; set; }

	public List<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
}