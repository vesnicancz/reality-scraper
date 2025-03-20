using System.ComponentModel.DataAnnotations;

namespace RealityScraper.Model;

// Model inzerátu
public class Listing
{
	[Key]
	public int Id { get; set; }

	public string ExternalId { get; set; }

	public string Title { get; set; }

	public string Location { get; set; }

	public decimal? Price { get; set; }

	public string Url { get; set; }

	public string ImageUrl { get; set; }

	public DateTime DiscoveredAt { get; set; }

	public DateTime LastSeenAt { get; set; }

	public List<PriceHistory> PriceHistories { get; set; }
}