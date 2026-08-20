using RealityScraper.Web.Shared.Models.Listings;

namespace RealityScraper.Web.Shared.Models.Dashboard;

public class DashboardSummaryResult
{
	public int WindowDays { get; set; }

	public int ActiveCount { get; set; }

	public int NewCount { get; set; }

	public int RemovedCount { get; set; }

	public int PriceDropCount { get; set; }

	public List<ListingResult> LatestListings { get; set; } = [];

	public List<PriceDropResult> RecentPriceDrops { get; set; } = [];
}