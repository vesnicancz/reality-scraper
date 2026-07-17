using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Application.Features.Listings;

internal static class ListingMapper
{
	public static ListingDto MapToDto(Listing entity, string? scraperTaskName)
	{
		return new ListingDto
		{
			Id = entity.Id,
			Title = entity.Title,
			Location = entity.Location,
			Price = entity.Price,
			Url = entity.Url,
			ImageUrl = entity.ImageUrl,
			CreatedAt = entity.CreatedAt,
			LastSeenAt = entity.LastSeenAt,
			RemovedAt = entity.RemovedAt,
			ScraperTaskId = entity.ScraperTaskId,
			ScraperTaskName = scraperTaskName
		};
	}

	public static PriceHistoryDto MapToPriceHistoryDto(PriceHistory entity)
	{
		return new PriceHistoryDto
		{
			Price = entity.Price,
			RecordedAt = entity.RecordedAt
		};
	}
}
