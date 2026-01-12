using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ScraperTaskTargets;
using RealityScraper.Application.Features.ScraperTaskTargets.Create;
using RealityScraper.Web.Api.Infrastructure;
using RealityScraper.Web.Api.Mappers.ScraperTaskTargets;
using RealityScraper.Web.Shared.Models.ScraperTaskTargets;

namespace RealityScraper.Web.Api.Endpoints.ScraperTaskTargets;

public class CreateScraperTaskTargetEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("/api/scraper-task-targets", async (
			CreateScraperTaskTargetRequest request,
			ICommandHandler<CreateScraperTaskTargetCommand, ScraperTaskTargetDto> commandHandler,
			CancellationToken cancellationToken) =>
		{
			var command = new CreateScraperTaskTargetCommand(request.ScraperTaskId, request.ScraperType, request.Url);

			var result = await commandHandler.Handle(command, cancellationToken);

			return result.IsSuccess
				? Results.Created($"/api/scraper-task-target/{result.Value.Id}", ScraperTaskTargetDtoMapper.MapToResult(result.Value))
				: CustomResults.Problem(result);
		});
	}
}