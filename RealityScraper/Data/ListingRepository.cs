using Microsoft.EntityFrameworkCore;
using RealityScraper.Model;

namespace RealityScraper.Data;

public class ListingRepository : IListingRepository
{
	private readonly RealityDbContext realityDbContext;

	public ListingRepository(RealityDbContext realityDbContext)
	{
		this.realityDbContext = realityDbContext;
	}

	public async Task<Listing> GetByExternalIdAsync(long externalId)
	{
		return await realityDbContext.Listings
			.Where(l => l.ExternalId == externalId)
			.FirstOrDefaultAsync();
	}
}