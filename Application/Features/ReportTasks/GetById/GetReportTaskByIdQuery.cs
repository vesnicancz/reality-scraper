using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.ReportTasks.GetById;

public record GetReportTaskByIdQuery(Guid Id) : IQuery<ReportTaskDto>;
