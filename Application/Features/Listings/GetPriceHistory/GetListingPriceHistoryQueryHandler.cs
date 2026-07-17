using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.Listings.GetPriceHistory;

internal sealed class GetListingPriceHistoryQueryHandler : IQueryHandler<GetListingPriceHistoryQuery, List<PriceHistoryDto>>
{
	private readonly IListingRepository listingRepository;

	public GetListingPriceHistoryQueryHandler(IListingRepository listingRepository)
	{
		this.listingRepository = listingRepository;
	}

	public async Task<Result<List<PriceHistoryDto>>> Handle(GetListingPriceHistoryQuery query, CancellationToken cancellationToken)
	{
		var listing = await listingRepository.GetWithPriceHistoryAsync(query.ListingId, cancellationToken);
		if (listing == null)
		{
			return Result.Failure<List<PriceHistoryDto>>(Error.NotFound("Listing.NotFound", $"Listing with ID {query.ListingId} was not found."));
		}

		var result = listing.PriceHistories
			.OrderBy(h => h.RecordedAt)
			.Select(ListingMapper.MapToPriceHistoryDto)
			.ToList();

		return Result.Success(result);
	}
}
