using RealityScraper.Application.Features.ReportTasks;
using RealityScraper.Web.Shared.Models.ReportTasks;

namespace RealityScraper.Web.Api.Mappers.ReportTasks;

public static class ReportTaskDtoMapper
{
	public static ReportTaskResult MapToResult(ReportTaskDto dto)
	{
		return new ReportTaskResult
		{
			Id = dto.Id,
			Name = dto.Name,
			CronExpression = dto.CronExpression,
			Enabled = dto.Enabled,
			LastRunAt = dto.LastRunAt,
			NextRunAt = dto.NextRunAt,
			LastRunSucceeded = dto.LastRunSucceeded,
			LastRunLog = dto.LastRunLog,
			LastSuccessfulReportAt = dto.LastSuccessfulReportAt,
			Recipients = dto.Recipients,
			Sources = dto.Sources.Select(s => new ReportTaskSourceResult
			{
				ScraperTaskId = s.ScraperTaskId,
				ScraperTaskName = s.ScraperTaskName
			}).ToList()
		};
	}
}
