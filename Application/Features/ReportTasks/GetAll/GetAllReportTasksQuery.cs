using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.ReportTasks.GetAll;

public record GetAllReportTasksQuery : IQuery<List<ReportTaskDto>>;
