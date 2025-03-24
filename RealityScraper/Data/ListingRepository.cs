using Microsoft.EntityFrameworkCore;
using RealityScraper.Model;

namespace RealityScraper.Data;

public class ListingRepository : IListingRepository
{
	private readonly DbSet<Listing> listingDbSet;

	public ListingRepository(RealityDbContext realityDbContext)
	{
		listingDbSet = realityDbContext.Set<Listing>();
	}

	public async Task<Listing> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken)
	{
		return await listingDbSet
			.Where(l => l.ExternalId == externalId)
			.Include(i => i.PriceHistories)
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task AddAsync(Listing listing, CancellationToken cancellationToken)
	{
		await listingDbSet.AddAsync(listing, cancellationToken);
	}
}