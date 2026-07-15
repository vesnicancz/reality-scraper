using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ReportTasks.Delete;
using RealityScraper.Web.Api.Infrastructure;

namespace RealityScraper.Web.Api.Endpoints.ReportTasks;

internal sealed class DeleteReportTaskEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapDelete("/api/report-tasks/{id:guid}", async (
			Guid id,
			ICommandHandler<DeleteReportTaskCommand> commandHandler,
			CancellationToken cancellationToken) =>
		{
			var command = new DeleteReportTaskCommand(id);

			var result = await commandHandler.Handle(command, cancellationToken);

			return result.IsSuccess
				? Results.NoContent()
				: CustomResults.Problem(result);
		});
	}
}
