using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Application.Interfaces.Repositories.Realty;

public interface IListingRepository
	: IRepository<Listing>
{
	Task<Listing?> GetByExternalIdAsync(Guid scraperTaskId, string externalId, CancellationToken cancellationToken);

	Task<List<Listing>> GetByScraperTaskIdAsync(Guid scraperTaskId, CancellationToken cancellationToken);

	/// <summary>
	/// Živé inzeráty (dosud nevyřazené) se zadanou URL obrázku, napříč všemi scraper úlohami.
	/// </summary>
	Task<List<Listing>> GetActiveWithImageUrlAsync(CancellationToken cancellationToken);

	Task<List<Listing>> GetRemovedInPeriodAsync(Guid scraperTaskId, DateTimeOffset fromExclusive, DateTimeOffset toInclusive, CancellationToken cancellationToken);

	Task<(List<Listing> Items, int TotalCount)> GetPagedAsync(bool? isActive, Guid? scraperTaskId, string? searchTerm, int skip, int take, CancellationToken cancellationToken);

	Task<Listing?> GetWithPriceHistoryAsync(Guid id, CancellationToken cancellationToken);

	/// <summary>
	/// Souhrnná čísla pro dashboard v klouzavém okně od <paramref name="since"/> do teď.
	/// </summary>
	Task<ListingDashboardStats> GetDashboardStatsAsync(DateTimeOffset since, CancellationToken cancellationToken);

	/// <summary>
	/// Živé inzeráty, kterým od <paramref name="since"/> klesla cena, od nejčerstvějšího zlevnění.
	/// </summary>
	Task<List<ListingPriceDrop>> GetRecentPriceDropsAsync(DateTimeOffset since, int take, CancellationToken cancellationToken);
}