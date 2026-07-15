namespace RealityScraper.Application.Features.ReportTasks;

public interface IReportTaskCommand
{
	string Name { get; }
	string CronExpression { get; }
	bool Enabled { get; }
	List<ReportTaskRecipientInput> Recipients { get; }
	List<Guid> ScraperTaskIds { get; }
}
