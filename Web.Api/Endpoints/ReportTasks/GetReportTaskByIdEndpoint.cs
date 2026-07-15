using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ReportTasks;
using RealityScraper.Application.Features.ReportTasks.GetById;
using RealityScraper.Web.Api.Infrastructure;
using RealityScraper.Web.Api.Mappers.ReportTasks;

namespace RealityScraper.Web.Api.Endpoints.ReportTasks;

internal sealed class GetReportTaskByIdEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet("/api/report-tasks/{id:guid}", async (
			Guid id,
			IQueryHandler<GetReportTaskByIdQuery, ReportTaskDto> queryHandler,
			CancellationToken cancellationToken) =>
		{
			var query = new GetReportTaskByIdQuery(id);

			var result = await queryHandler.Handle(query, cancellationToken);

			return result.IsSuccess
				? Results.Ok(ReportTaskDtoMapper.MapToResult(result.Value))
				: CustomResults.Problem(result);
		});
	}
}
