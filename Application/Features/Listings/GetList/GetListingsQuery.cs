using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.Listings.GetList;

public record GetListingsQuery(bool? IsActive, Guid? ScraperTaskId, string? SearchTerm, int PageIndex, int PageSize) : IQuery<ListingPageDto>;
