using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ReportTasks;
using RealityScraper.Application.Features.ReportTasks.Create;
using RealityScraper.Web.Api.Infrastructure;
using RealityScraper.Web.Api.Mappers.ReportTasks;
using RealityScraper.Web.Shared.Models.ReportTasks;

namespace RealityScraper.Web.Api.Endpoints.ReportTasks;

internal sealed class CreateReportTaskEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("/api/report-tasks", async (
			CreateReportTaskRequest request,
			ICommandHandler<CreateReportTaskCommand, ReportTaskDto> commandHandler,
			CancellationToken cancellationToken) =>
		{
			var command = new CreateReportTaskCommand(
				request.Name,
				request.CronExpression,
				request.Enabled,
				(request.Recipients ?? []).Select(r => new ReportTaskRecipientInput(r.Email)).ToList(),
				request.ScraperTaskIds ?? []);

			var result = await commandHandler.Handle(command, cancellationToken);

			return result.IsSuccess
				? Results.Created($"/api/report-tasks/{result.Value.Id}", ReportTaskDtoMapper.MapToResult(result.Value))
				: CustomResults.Problem(result);
		});
	}
}
