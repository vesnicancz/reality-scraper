using RealityScraper.Domain.Common;

namespace RealityScraper.Domain.Entities.Realty;

public class Listing : BaseEntity
{
	public string ExternalId { get; set; }

	public string Title { get; set; }

	public string Location { get; set; }

	public decimal? Price { get; set; }

	public string Url { get; set; }

	public string ImageUrl { get; set; }

	public DateTime CreatedAt { get; set; }

	public DateTime LastSeenAt { get; set; }

	public DateTime PriceFrom { get; set; }

	public List<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
}