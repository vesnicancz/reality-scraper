using RealityScraper.Application.Features.ScraperTaskTargets;
using RealityScraper.Web.Shared.Models.ScraperTaskTargets;

namespace RealityScraper.Web.Api.Mappers.ScraperTaskTargets;

public static class ScraperTaskTargetDtoMapper
{
	public static ScraperTaskTargetResult MapToResult(ScraperTaskTargetDto dto)
	{
		return new ScraperTaskTargetResult()
		{
			Id = dto.Id,
			ScraperTaskId = dto.ScraperTaskId,
			ScraperType = dto.ScraperType,
			Url = dto.Url
		};
	}
}