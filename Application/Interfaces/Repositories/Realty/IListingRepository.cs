using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Application.Interfaces.Repositories.Realty;

public interface IListingRepository
	: IRepository<Listing>
{
	Task<Listing?> GetByExternalIdAsync(Guid scraperTaskId, string externalId, CancellationToken cancellationToken);

	Task<List<Listing>> GetByScraperTaskIdAsync(Guid scraperTaskId, CancellationToken cancellationToken);

	Task<List<Listing>> GetRemovedInPeriodAsync(Guid scraperTaskId, DateTimeOffset fromExclusive, DateTimeOffset toInclusive, CancellationToken cancellationToken);
}