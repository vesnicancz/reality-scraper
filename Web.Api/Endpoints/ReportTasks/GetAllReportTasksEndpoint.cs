using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ReportTasks;
using RealityScraper.Application.Features.ReportTasks.GetAll;
using RealityScraper.Web.Api.Infrastructure;
using RealityScraper.Web.Api.Mappers.ReportTasks;

namespace RealityScraper.Web.Api.Endpoints.ReportTasks;

internal sealed class GetAllReportTasksEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet("/api/report-tasks", async (
			IQueryHandler<GetAllReportTasksQuery, List<ReportTaskDto>> queryHandler,
			CancellationToken cancellationToken) =>
		{
			var query = new GetAllReportTasksQuery();

			var result = await queryHandler.Handle(query, cancellationToken);

			return result.IsSuccess
				? Results.Ok(result.Value.Select(ReportTaskDtoMapper.MapToResult).ToList())
				: CustomResults.Problem(result);
		});
	}
}
