using Microsoft.EntityFrameworkCore;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.Persistence.Contexts;

namespace RealityScraper.Persistence.Repositories.Realty;

public class ListingRepository : Repository<Listing>, IListingRepository
{
	public ListingRepository(IDbContext dbContext)
		: base(dbContext)
	{
	}

	public Task<Listing?> GetByExternalIdAsync(Guid scraperTaskId, string externalId, CancellationToken cancellationToken)
	{
		return dbContext
			.Set<Listing>()
			.FirstOrDefaultAsync(x => x.ScraperTaskId == scraperTaskId && x.ExternalId == externalId, cancellationToken);
	}
}