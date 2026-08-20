using RealityScraper.Application.Features.Listings;

namespace RealityScraper.Application.Features.Dashboard;

public class DashboardSummaryDto
{
	/// <summary>
	/// Délka klouzavého okna ve dnech, ze kterého jsou počítané změnové ukazatele.
	/// </summary>
	public int WindowDays { get; set; }

	public int ActiveCount { get; set; }

	public int NewCount { get; set; }

	public int RemovedCount { get; set; }

	public int PriceDropCount { get; set; }

	public List<ListingDto> LatestListings { get; set; } = [];

	public List<PriceDropDto> RecentPriceDrops { get; set; } = [];
}