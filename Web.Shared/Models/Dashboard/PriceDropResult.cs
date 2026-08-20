using RealityScraper.Web.Shared.Models.Listings;

namespace RealityScraper.Web.Shared.Models.Dashboard;

public class PriceDropResult
{
	public ListingResult Listing { get; set; } = null!;

	public decimal PreviousPrice { get; set; }
}