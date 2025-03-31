using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Application.Interfaces.Repositories.Realty;

public interface IListingRepository
	: IRepository<Listing>
{
	Task<Listing?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken);
}