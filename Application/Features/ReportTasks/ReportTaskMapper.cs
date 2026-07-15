using RealityScraper.Domain.Entities.Tasks;

namespace RealityScraper.Application.Features.ReportTasks;

internal static class ReportTaskMapper
{
	public static ReportTaskDto MapToListDto(RemovedListingsReportTask entity)
	{
		return new ReportTaskDto
		{
			Id = entity.Id,
			Name = entity.Name,
			CronExpression = entity.CronExpression,
			Enabled = entity.Enabled,
			LastRunAt = entity.LastRunAt,
			NextRunAt = entity.NextRunAt,
			LastRunSucceeded = entity.LastRunSucceeded,
			LastSuccessfulReportAt = entity.LastSuccessfulReportAt
		};
	}

	public static ReportTaskDto MapToDetailDto(RemovedListingsReportTask entity)
	{
		var dto = MapToListDto(entity);

		dto.LastRunLog = entity.LastRunLog;

		dto.Recipients = entity.Recipients.Select(r => r.Email).ToList();

		dto.Sources = entity.Sources.Select(s => new ReportTaskSourceDto
		{
			ScraperTaskId = s.ScraperTaskId,
			ScraperTaskName = s.ScraperTask?.Name ?? string.Empty
		}).ToList();

		return dto;
	}
}
