namespace RealityScraper.Domain.Entities.Realty;

public class PriceHistory
{
	public Guid Id { get; set; }

	public Guid ListingId { get; set; }

	public Listing Listing { get; set; }

	public decimal? Price { get; set; }

	public DateTime RecordedAt { get; set; }
}