using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ReportTasks;
using RealityScraper.Application.Features.ReportTasks.Update;
using RealityScraper.Web.Api.Infrastructure;
using RealityScraper.Web.Api.Mappers.ReportTasks;
using RealityScraper.Web.Shared.Models.ReportTasks;

namespace RealityScraper.Web.Api.Endpoints.ReportTasks;

internal sealed class UpdateReportTaskEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPut("/api/report-tasks/{id:guid}", async (
			Guid id,
			UpdateReportTaskRequest request,
			ICommandHandler<UpdateReportTaskCommand, ReportTaskDto> commandHandler,
			CancellationToken cancellationToken) =>
		{
			var command = new UpdateReportTaskCommand(
				id,
				request.Name,
				request.CronExpression,
				request.Enabled,
				(request.Recipients ?? []).Select(r => new ReportTaskRecipientInput(r.Email)).ToList(),
				request.ScraperTaskIds ?? []);

			var result = await commandHandler.Handle(command, cancellationToken);

			return result.IsSuccess
				? Results.Ok(ReportTaskDtoMapper.MapToResult(result.Value))
				: CustomResults.Problem(result);
		});
	}
}
