using RealityScraper.Domain.Common;

namespace RealityScraper.Domain.Entities.Realty;

public class PriceHistory : BaseEntity
{
	public Guid ListingId { get; set; }

	public Listing Listing { get; set; }

	public decimal? Price { get; set; }

	public DateTime RecordedAt { get; set; }
}