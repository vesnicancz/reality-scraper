using RealityScraper.Application.Features.Maintenance.BackfillListingImages;

namespace RealityScraper.Web.Api.Mappers.Maintenance;

public static class BackfillListingImagesDtoMapper
{
	public static Web.Shared.Models.Maintenance.BackfillListingImagesResult MapToResult(
		BackfillListingImagesResult dto)
	{
		return new Web.Shared.Models.Maintenance.BackfillListingImagesResult
		{
			CheckedCount = dto.CheckedCount,
			DownloadedCount = dto.DownloadedCount,
			FailedCount = dto.FailedCount,
			RemainingCount = dto.RemainingCount
		};
	}
}
