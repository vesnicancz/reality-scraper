namespace RealityScraper.Model;

public class PriceHistory
{
	public int Id { get; set; }

	public int ListingId { get; set; }

	public decimal Price { get; set; }

	public DateTime DateFrom { get; set; }
}