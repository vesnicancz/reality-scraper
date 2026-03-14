using RealityScraper.SharedKernel;

namespace RealityScraper.Domain.Entities.Realty;

public class PriceHistory : Entity
{
	public Guid ListingId { get; set; }

	public Listing Listing { get; set; }

	public decimal? Price { get; set; }

	public DateTimeOffset RecordedAt { get; set; }
}