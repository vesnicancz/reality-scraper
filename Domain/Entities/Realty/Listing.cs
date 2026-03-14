using RealityScraper.SharedKernel;

namespace RealityScraper.Domain.Entities.Realty;

public class Listing : Entity
{
	public string ExternalId { get; set; }

	public string Title { get; set; }

	public string Location { get; set; }

	public decimal? Price { get; set; }

	public string Url { get; set; }

	public string ImageUrl { get; set; }

	public DateTimeOffset CreatedAt { get; set; }

	public DateTimeOffset LastSeenAt { get; set; }

	public DateTimeOffset PriceFrom { get; set; }

	public Guid? ScraperTaskId { get; set; }

	public List<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
}