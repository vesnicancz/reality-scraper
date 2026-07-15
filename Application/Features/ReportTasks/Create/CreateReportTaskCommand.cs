using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.ReportTasks.Create;

public record CreateReportTaskCommand(
	string Name,
	string CronExpression,
	bool Enabled,
	List<ReportTaskRecipientInput> Recipients,
	List<Guid> ScraperTaskIds) : ICommand<ReportTaskDto>, IReportTaskCommand;
