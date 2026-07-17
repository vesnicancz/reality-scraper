using RealityScraper.Application.Features.Listings;
using RealityScraper.Web.Shared.Models.Listings;

namespace RealityScraper.Web.Api.Mappers.Listings;

public static class ListingDtoMapper
{
	public static ListingPageResult MapToResult(ListingPageDto dto)
	{
		return new ListingPageResult
		{
			Items = dto.Items.Select(MapToResult).ToList(),
			TotalCount = dto.TotalCount
		};
	}

	public static ListingResult MapToResult(ListingDto dto)
	{
		return new ListingResult
		{
			Id = dto.Id,
			Title = dto.Title,
			Location = dto.Location,
			Price = dto.Price,
			Url = dto.Url,
			ImageUrl = dto.ImageUrl,
			CreatedAt = dto.CreatedAt,
			LastSeenAt = dto.LastSeenAt,
			RemovedAt = dto.RemovedAt,
			ScraperTaskId = dto.ScraperTaskId,
			ScraperTaskName = dto.ScraperTaskName
		};
	}

	public static PriceHistoryResult MapToResult(PriceHistoryDto dto)
	{
		return new PriceHistoryResult
		{
			Price = dto.Price,
			RecordedAt = dto.RecordedAt
		};
	}
}
