using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ScraperTasks.Delete;
using RealityScraper.Web.Api.Infrastructure;

namespace RealityScraper.Web.Api.Endpoints.ScraperTasks;

internal sealed class DeleteScraperTaskEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapDelete("/api/scraper-tasks/{id:guid}", async (
			Guid id,
			ICommandHandler<DeleteScraperTaskCommand> commandHandler,
			CancellationToken cancellationToken) =>
		{
			var command = new DeleteScraperTaskCommand(id);

			var result = await commandHandler.Handle(command, cancellationToken);

			return result.IsSuccess
				? Results.NoContent()
				: CustomResults.Problem(result);
		});
	}
}