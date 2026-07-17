using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.Listings.GetList;

internal sealed class GetListingsQueryHandler : IQueryHandler<GetListingsQuery, ListingPageDto>
{
	private const int MaxPageSize = 100;

	private readonly IListingRepository listingRepository;
	private readonly IScraperTaskRepository scraperTaskRepository;

	public GetListingsQueryHandler(IListingRepository listingRepository, IScraperTaskRepository scraperTaskRepository)
	{
		this.listingRepository = listingRepository;
		this.scraperTaskRepository = scraperTaskRepository;
	}

	public async Task<Result<ListingPageDto>> Handle(GetListingsQuery query, CancellationToken cancellationToken)
	{
		var pageIndex = Math.Max(query.PageIndex, 0);
		var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

		var (listings, totalCount) = await listingRepository.GetPagedAsync(
			query.IsActive,
			query.ScraperTaskId,
			query.SearchTerm,
			pageIndex * pageSize,
			pageSize,
			cancellationToken);

		var scraperTasks = await scraperTaskRepository.GetAllAsync(cancellationToken);
		var taskNamesById = scraperTasks.ToDictionary(t => t.Id, t => t.Name);

		var result = new ListingPageDto
		{
			Items = listings
				.Select(l => ListingMapper.MapToDto(
					l,
					l.ScraperTaskId.HasValue && taskNamesById.TryGetValue(l.ScraperTaskId.Value, out var name) ? name : null))
				.ToList(),
			TotalCount = totalCount
		};

		return Result.Success(result);
	}
}
