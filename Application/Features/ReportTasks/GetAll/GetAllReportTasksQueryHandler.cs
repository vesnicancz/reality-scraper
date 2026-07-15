using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.ReportTasks.GetAll;

internal sealed class GetAllReportTasksQueryHandler : IQueryHandler<GetAllReportTasksQuery, List<ReportTaskDto>>
{
	private readonly IReportTaskRepository reportTaskRepository;

	public GetAllReportTasksQueryHandler(IReportTaskRepository reportTaskRepository)
	{
		this.reportTaskRepository = reportTaskRepository;
	}

	public async Task<Result<List<ReportTaskDto>>> Handle(GetAllReportTasksQuery query, CancellationToken cancellationToken)
	{
		var tasks = await reportTaskRepository.GetAllAsync(cancellationToken);

		var result = tasks.Select(t => ReportTaskMapper.MapToListDto(t)).ToList();

		return Result.Success(result);
	}
}
