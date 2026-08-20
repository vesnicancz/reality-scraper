using RealityScraper.Application.Features.Dashboard;
using RealityScraper.Web.Api.Mappers.Listings;
using RealityScraper.Web.Shared.Models.Dashboard;

namespace RealityScraper.Web.Api.Mappers.Dashboard;

public static class DashboardDtoMapper
{
	public static DashboardSummaryResult MapToResult(DashboardSummaryDto dto)
	{
		return new DashboardSummaryResult
		{
			WindowDays = dto.WindowDays,
			ActiveCount = dto.ActiveCount,
			NewCount = dto.NewCount,
			RemovedCount = dto.RemovedCount,
			PriceDropCount = dto.PriceDropCount,
			LatestListings = dto.LatestListings.Select(ListingDtoMapper.MapToResult).ToList(),
			RecentPriceDrops = dto.RecentPriceDrops.Select(MapToResult).ToList()
		};
	}

	public static PriceDropResult MapToResult(PriceDropDto dto)
	{
		return new PriceDropResult
		{
			Listing = ListingDtoMapper.MapToResult(dto.Listing),
			PreviousPrice = dto.PreviousPrice
		};
	}
}