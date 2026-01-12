using RealityScraper.Application.Features.ScraperTaskRecipients;
using RealityScraper.Web.Shared.Models.ScraperTaskRecipients;

namespace RealityScraper.Web.Api.Mappers.ScraperTaskRecipients;

public static class ScraperTaskRecipientDtoMapper
{
	public static ScraperTaskRecipientResult MapToResult(ScraperTaskRecipientDto dto)
	{
		return new ScraperTaskRecipientResult
		{
			Id = dto.Id,
			ScraperTaskId = dto.ScraperTaskId,
			Email = dto.Email
		};
	}
}