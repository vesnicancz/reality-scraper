using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ReportTasks.GetById;

internal sealed class GetReportTaskByIdQueryHandler : IQueryHandler<GetReportTaskByIdQuery, ReportTaskDto>
{
	private readonly IReportTaskRepository reportTaskRepository;

	public GetReportTaskByIdQueryHandler(IReportTaskRepository reportTaskRepository)
	{
		this.reportTaskRepository = reportTaskRepository;
	}

	public async Task<Result<ReportTaskDto>> Handle(GetReportTaskByIdQuery query, CancellationToken cancellationToken)
	{
		var reportTask = await reportTaskRepository.GetTaskWithDetailsAsync(query.Id, cancellationToken);
		if (reportTask == null)
		{
			return Result.Failure<ReportTaskDto>(Error.NotFound("ReportTask.NotFound", $"ReportTask with ID {query.Id} was not found."));
		}

		return Result.Success(ReportTaskMapper.MapToDetailDto(reportTask));
	}
}
