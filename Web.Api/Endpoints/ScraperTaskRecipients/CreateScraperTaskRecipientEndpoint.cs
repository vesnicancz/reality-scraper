using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.ScraperTaskRecipients;
using RealityScraper.Application.Features.ScraperTaskRecipients.Create;
using RealityScraper.Web.Api.Infrastructure;
using RealityScraper.Web.Api.Mappers.ScraperTaskRecipients;
using RealityScraper.Web.Shared.Models.ScraperTaskRecipients;

namespace RealityScraper.Web.Api.Endpoints.ScraperTaskRecipients;

internal sealed class CreateScraperTaskRecipientEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("/api/scraper-task-recipients", async (
			CreateScraperTaskRecipientRequest request,
			ICommandHandler<CreateScraperTaskRecipientCommand, ScraperTaskRecipientDto> commandHandler,
			CancellationToken cancellationToken) =>
		{
			var command = new CreateScraperTaskRecipientCommand(request.ScraperTaskId, request.Email);

			var result = await commandHandler.Handle(command, cancellationToken);

			return result.IsSuccess
				? Results.Created($"/api/scraper-task-recipients/{result.Value.Id}", ScraperTaskRecipientDtoMapper.MapToResult(result.Value))
				: CustomResults.Problem(result);
		});
	}
}