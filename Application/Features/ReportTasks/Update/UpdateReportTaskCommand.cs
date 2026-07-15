using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.ReportTasks.Update;

public record UpdateReportTaskCommand(
	Guid Id,
	string Name,
	string CronExpression,
	bool Enabled,
	List<ReportTaskRecipientInput> Recipients,
	List<Guid> ScraperTaskIds) : ICommand<ReportTaskDto>, IReportTaskCommand;
