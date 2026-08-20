using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.Maintenance.BackfillListingImages;
using RealityScraper.Web.Api.Infrastructure;
using RealityScraper.Web.Api.Mappers.Maintenance;

namespace RealityScraper.Web.Api.Endpoints.Maintenance;

internal sealed class BackfillListingImagesEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("/api/maintenance/listing-images/backfill", async (
			ICommandHandler<BackfillListingImagesCommand, BackfillListingImagesResult> commandHandler,
			CancellationToken cancellationToken) =>
		{
			var result = await commandHandler.Handle(new BackfillListingImagesCommand(), cancellationToken);

			return result.IsSuccess
				? Results.Ok(BackfillListingImagesDtoMapper.MapToResult(result.Value))
				: CustomResults.Problem(result);
		});
	}
}
