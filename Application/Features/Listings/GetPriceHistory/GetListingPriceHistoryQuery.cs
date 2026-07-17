using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.Listings.GetPriceHistory;

public record GetListingPriceHistoryQuery(Guid ListingId) : IQuery<List<PriceHistoryDto>>;
