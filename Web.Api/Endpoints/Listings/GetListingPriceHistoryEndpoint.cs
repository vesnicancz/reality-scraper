using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.Listings;
using RealityScraper.Application.Features.Listings.GetPriceHistory;
using RealityScraper.Web.Api.Infrastructure;
using RealityScraper.Web.Api.Mappers.Listings;

namespace RealityScraper.Web.Api.Endpoints.Listings;

internal sealed class GetListingPriceHistoryEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet("/api/listings/{id:guid}/price-history", async (
			Guid id,
			IQueryHandler<GetListingPriceHistoryQuery, List<PriceHistoryDto>> queryHandler,
			CancellationToken cancellationToken) =>
		{
			var query = new GetListingPriceHistoryQuery(id);

			var result = await queryHandler.Handle(query, cancellationToken);

			return result.IsSuccess
				? Results.Ok(result.Value.Select(ListingDtoMapper.MapToResult).ToList())
				: CustomResults.Problem(result);
		});
	}
}
