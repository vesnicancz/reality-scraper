namespace RealityScraper.Application.Features.Listings;

public class PriceHistoryDto
{
	public decimal? Price { get; set; }

	public DateTimeOffset RecordedAt { get; set; }

	public bool IsCurrent { get; set; }
}
