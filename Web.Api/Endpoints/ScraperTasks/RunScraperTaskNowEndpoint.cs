using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ScraperTasks.RunNow;
using RealityScraper.Web.Api.Infrastructure;

namespace RealityScraper.Web.Api.Endpoints.ScraperTasks;

internal sealed class RunScraperTaskNowEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("/api/scraper-tasks/{id:guid}/run-now", async (
			Guid id,
			ICommandHandler<RunTaskNowCommand> commandHandler,
			CancellationToken cancellationToken) =>
		{
			var command = new RunTaskNowCommand(id);

			var result = await commandHandler.Handle(command, cancellationToken);

			return result.IsSuccess
				? Results.Accepted()
				: CustomResults.Problem(result);
		});
	}
}