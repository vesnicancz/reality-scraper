using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Features.Listings;
using RealityScraper.Application.Interfaces.Repositories.Configuration;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.Dashboard.GetSummary;

internal sealed class GetDashboardSummaryQueryHandler : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
	/// <summary>Klouzavé okno změnových ukazatelů – posledních 168 hodin, ne kalendářní týden.</summary>
	private const int WindowDays = 7;

	/// <summary>
	/// Počet položek v obou seznamech na dashboardu. Nejnovější inzeráty a poslední zlevnění
	/// stojí vedle sebe a mají stejně vysoké řádky, takže se drží na společném čísle - jinak
	/// jeden sloupec bezdůvodně přesahuje druhý.
	/// </summary>
	private const int ListingsPerPanel = 6;

	private readonly IListingRepository listingRepository;
	private readonly IScraperTaskRepository scraperTaskRepository;
	private readonly IDateTimeProvider dateTimeProvider;

	public GetDashboardSummaryQueryHandler(
		IListingRepository listingRepository,
		IScraperTaskRepository scraperTaskRepository,
		IDateTimeProvider dateTimeProvider)
	{
		this.listingRepository = listingRepository;
		this.scraperTaskRepository = scraperTaskRepository;
		this.dateTimeProvider = dateTimeProvider;
	}

	public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery query, CancellationToken cancellationToken)
	{
		var since = dateTimeProvider.UtcNow.AddDays(-WindowDays);

		var stats = await listingRepository.GetDashboardStatsAsync(since, cancellationToken);

		// Nejnovější inzeráty umí stránkovací dotaz – řadí CreatedAt DESC, takže stačí první stránka.
		var (latestListings, _) = await listingRepository.GetPagedAsync(
			isActive: true,
			scraperTaskId: null,
			searchTerm: null,
			skip: 0,
			take: ListingsPerPanel,
			cancellationToken);

		var priceDrops = await listingRepository.GetRecentPriceDropsAsync(since, ListingsPerPanel, cancellationToken);

		var scraperTasks = await scraperTaskRepository.GetAllAsync(cancellationToken);
		var taskNamesById = scraperTasks.ToDictionary(t => t.Id, t => t.Name);

		var result = new DashboardSummaryDto
		{
			WindowDays = WindowDays,
			ActiveCount = stats.ActiveCount,
			NewCount = stats.NewCount,
			RemovedCount = stats.RemovedCount,
			PriceDropCount = stats.PriceDropCount,
			LatestListings = latestListings
				.Select(l => ListingMapper.MapToDto(l, GetTaskName(taskNamesById, l.ScraperTaskId)))
				.ToList(),
			RecentPriceDrops = priceDrops
				.Select(d => new PriceDropDto
				{
					Listing = ListingMapper.MapToDto(d.Listing, GetTaskName(taskNamesById, d.Listing.ScraperTaskId)),
					PreviousPrice = d.PreviousPrice
				})
				.ToList()
		};

		return Result.Success(result);
	}

	private static string? GetTaskName(Dictionary<Guid, string> taskNamesById, Guid? scraperTaskId)
	{
		return scraperTaskId.HasValue && taskNamesById.TryGetValue(scraperTaskId.Value, out var name) ? name : null;
	}
}