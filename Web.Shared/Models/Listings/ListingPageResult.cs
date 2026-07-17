namespace RealityScraper.Web.Shared.Models.Listings;

public class ListingPageResult
{
	public List<ListingResult> Items { get; set; } = [];

	public int TotalCount { get; set; }
}
