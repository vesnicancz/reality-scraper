using RealityScraper.Model;

namespace RealityScraper.Data;

public interface IListingRepository
{
	Task<Listing> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken);

	Task AddAsync(Listing listing, CancellationToken cancellationToken);
}