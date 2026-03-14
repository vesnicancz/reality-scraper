using RealityScraper.Application.Features.ScraperTaskRecipients;
using RealityScraper.Application.Features.ScraperTaskTargets;
using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Application.Features.ScraperTasks;

internal static class ScraperTaskMapper
{
	public static ScraperTaskDto MapToListDto(ScraperTask entity)
	{
		return new ScraperTaskDto
		{
			Id = entity.Id,
			Name = entity.Name,
			CronExpression = entity.CronExpression,
			Enabled = entity.Enabled,
			LastRunAt = entity.LastRunAt,
			NextRunAt = entity.NextRunAt
		};
	}

	public static ScraperTaskDto MapToDetailDto(ScraperTask entity)
	{
		var dto = MapToListDto(entity);

		dto.Recipients = entity.Recipients.Select(r => new ScraperTaskRecipientDto
		{
			Id = r.Id,
			ScraperTaskId = r.ScraperTaskId,
			Email = r.Email
		}).ToList();

		dto.Targets = entity.Targets.Select(t => new ScraperTaskTargetDto
		{
			Id = t.Id,
			ScraperTaskId = t.ScraperTaskId,
			ScraperType = (int)t.ScraperType,
			Url = t.Url
		}).ToList();

		return dto;
	}
}