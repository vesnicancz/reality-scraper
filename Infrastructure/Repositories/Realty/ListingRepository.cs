using Microsoft.EntityFrameworkCore;
using RealityScraper.Application.Abstractions.Database;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Infrastructure.Repositories.Realty;

internal class ListingRepository : Repository<Listing>, IListingRepository
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

	public Task<List<Listing>> GetByScraperTaskIdAsync(Guid scraperTaskId, CancellationToken cancellationToken)
	{
		return dbContext
			.Set<Listing>()
			.Where(x => x.ScraperTaskId == scraperTaskId)
			.ToListAsync(cancellationToken);
	}

	public async Task<(List<Listing> Items, int TotalCount)> GetPagedAsync(bool? isActive, Guid? scraperTaskId, string? searchTerm, int skip, int take, CancellationToken cancellationToken)
	{
		var query = dbContext
			.Set<Listing>()
			.AsNoTracking()
			.AsQueryable();

		if (isActive == true)
		{
			query = query.Where(x => x.RemovedAt == null);
		}
		else if (isActive == false)
		{
			query = query.Where(x => x.RemovedAt != null);
		}

		if (scraperTaskId.HasValue)
		{
			query = query.Where(x => x.ScraperTaskId == scraperTaskId.Value);
		}

		if (!string.IsNullOrWhiteSpace(searchTerm))
		{
			var pattern = $"%{EscapeLikePattern(searchTerm.Trim())}%";
			query = query.Where(x => EF.Functions.ILike(x.Title, pattern) || EF.Functions.ILike(x.Location, pattern));
		}

		var totalCount = await query.CountAsync(cancellationToken);

		var items = await query
			.OrderByDescending(x => x.CreatedAt)
			.ThenBy(x => x.Id)
			.Skip(skip)
			.Take(take)
			.ToListAsync(cancellationToken);

		return (items, totalCount);
	}

	public Task<Listing?> GetWithPriceHistoryAsync(Guid id, CancellationToken cancellationToken)
	{
		return dbContext
			.Set<Listing>()
			.AsNoTracking()
			.Include(x => x.PriceHistories)
			.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
	}

	private static string EscapeLikePattern(string value)
	{
		return value
			.Replace(@"\", @"\\")
			.Replace("%", @"\%")
			.Replace("_", @"\_");
	}

	public Task<List<Listing>> GetRemovedInPeriodAsync(Guid scraperTaskId, DateTimeOffset fromExclusive, DateTimeOffset toInclusive, CancellationToken cancellationToken)
	{
		return dbContext
			.Set<Listing>()
			.AsNoTracking()
			.Where(x => x.ScraperTaskId == scraperTaskId
				&& x.RemovedAt != null
				&& x.RemovedAt > fromExclusive
				&& x.RemovedAt <= toInclusive)
			.OrderByDescending(x => x.RemovedAt)
			.ToListAsync(cancellationToken);
	}
}