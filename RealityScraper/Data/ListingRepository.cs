﻿using Microsoft.EntityFrameworkCore;
using RealityScraper.Model;

namespace RealityScraper.Data;

public class ListingRepository : IListingRepository
{
	private readonly RealityDbContext realityDbContext;

	public ListingRepository(RealityDbContext realityDbContext)
	{
		this.realityDbContext = realityDbContext;
	}

	public async Task<Listing> GetByExternalIdAsync(string externalId)
	{
		return await realityDbContext.Listings
			.Where(l => l.ExternalId == externalId)
			.Include(i => i.PriceHistories)
			.FirstOrDefaultAsync();
	}
}