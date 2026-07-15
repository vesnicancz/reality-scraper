using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.ReportTasks.Delete;

public record DeleteReportTaskCommand(Guid Id) : ICommand;
