using RealityScraper.Application.Features.ScraperTasks;
using RealityScraper.Web.Shared.Models.ScraperTasks;

namespace RealityScraper.Web.Api.Mappers.ScraperTasks;

public static class ScraperTaskDtoMapper
{
	public static ScraperTaskResult MapToResult(ScraperTaskDto dto)
	{
		return new ScraperTaskResult
		{
			Id = dto.Id,
			Name = dto.Name,
			CronExpression = dto.CronExpression,
			Enabled = dto.Enabled
		};
	}
}