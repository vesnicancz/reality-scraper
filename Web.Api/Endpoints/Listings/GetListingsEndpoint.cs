using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.Listings;
using RealityScraper.Application.Features.Listings.GetList;
using RealityScraper.Web.Api.Infrastructure;
using RealityScraper.Web.Api.Mappers.Listings;

namespace RealityScraper.Web.Api.Endpoints.Listings;

internal sealed class GetListingsEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet("/api/listings", async (
			IQueryHandler<GetListingsQuery, ListingPageDto> queryHandler,
			CancellationToken cancellationToken,
			bool? isActive,
			Guid? scraperTaskId,
			string? search,
			int pageIndex = 0,
			int pageSize = 15) =>
		{
			var query = new GetListingsQuery(isActive, scraperTaskId, search, pageIndex, pageSize);

			var result = await queryHandler.Handle(query, cancellationToken);

			return result.IsSuccess
				? Results.Ok(ListingDtoMapper.MapToResult(result.Value))
				: CustomResults.Problem(result);
		});
	}
}
