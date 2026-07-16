using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ScraperTasks.RunNow;
using RealityScraper.Web.Api.Infrastructure;

namespace RealityScraper.Web.Api.Endpoints.ReportTasks;

internal sealed class RunReportTaskNowEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		// Run-now command pracuje nad TaskBase, funguje tedy pro všechny typy tasků
		app.MapPost("/api/report-tasks/{id:guid}/run-now", async (
			Guid id,
			ICommandHandler<RunScraperTaskNowCommand> commandHandler,
			CancellationToken cancellationToken) =>
		{
			var command = new RunScraperTaskNowCommand(id);

			var result = await commandHandler.Handle(command, cancellationToken);

			return result.IsSuccess
				? Results.Accepted()
				: CustomResults.Problem(result);
		});
	}
}
