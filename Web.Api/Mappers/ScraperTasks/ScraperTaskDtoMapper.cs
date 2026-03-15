using RealityScraper.Application.Features.ScraperTasks;
using RealityScraper.Web.Api.Mappers.ScraperTaskRecipients;
using RealityScraper.Web.Api.Mappers.ScraperTaskTargets;
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
			Enabled = dto.Enabled,
			LastRunAt = dto.LastRunAt,
			NextRunAt = dto.NextRunAt,
			LastRunSucceeded = dto.LastRunSucceeded,
			LastRunLog = dto.LastRunLog,
			Recipients = dto.Recipients.Select(ScraperTaskRecipientDtoMapper.MapToResult).ToList(),
			Targets = dto.Targets.Select(ScraperTaskTargetDtoMapper.MapToResult).ToList()
		};
	}
}