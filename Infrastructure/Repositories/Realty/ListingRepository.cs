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

	public Task<List<Listing>> GetActiveWithImageUrlAsync(CancellationToken cancellationToken)
	{
		return dbContext
			.Set<Listing>()
			.AsNoTracking()
			.Where(x => x.RemovedAt == null && x.ImageUrl != "")
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

	public async Task<ListingDashboardStats> GetDashboardStatsAsync(DateTimeOffset since, CancellationToken cancellationToken)
	{
		var listings = dbContext
			.Set<Listing>()
			.AsNoTracking();

		// Čtyři samostatné COUNTy místo jednoho GroupBy: EF Core nepřeloží Count s predikátem
		// uvnitř agregace a počet zlevnění navíc potřebuje korelovaný poddotaz. Tabulka má řádově
		// tisíce řádků, čtyři roundtripy jsou levnější než riziko nepřeložitelného dotazu.
		var activeCount = await listings.CountAsync(x => x.RemovedAt == null, cancellationToken);

		// Bez ohledu na RemovedAt – inzerát byl v okně nový, i když už mezitím zmizel.
		var newCount = await listings.CountAsync(x => x.CreatedAt >= since, cancellationToken);

		// Znovuobjevený inzerát má RemovedAt zpátky na null (RemovedListingDetector), takže tohle
		// je "aktuálně vyřazené, vyřazené v okně", ne "počet vyřazení v okně".
		var removedCount = await listings.CountAsync(x => x.RemovedAt != null && x.RemovedAt >= since, cancellationToken);

		var priceDropCount = await FilterPriceDropsSince(listings, since).CountAsync(cancellationToken);

		return new ListingDashboardStats(activeCount, newCount, removedCount, priceDropCount);
	}

	public async Task<List<ListingPriceDrop>> GetRecentPriceDropsAsync(DateTimeOffset since, int take, CancellationToken cancellationToken)
	{
		// Historie se dotahuje přes Include, protože jde o pár řádků – druhý korelovaný poddotaz
		// na předchozí cenu by byl dražší než načíst ji k vybraným inzerátům.
		var listings = await FilterPriceDropsSince(dbContext.Set<Listing>().AsNoTracking(), since)
			.OrderByDescending(x => x.PriceFrom)
			.ThenBy(x => x.Id)
			.Take(take)
			.Include(x => x.PriceHistories)
			.ToListAsync(cancellationToken);

		return listings
			.Select(x => new ListingPriceDrop(x, GetPreviousPrice(x)))
			.ToList();
	}

	/// <summary>
	/// Inzeráty, u kterých byla poslední cenová změna od <paramref name="since"/> zlevnění.
	/// PriceFrom je okamžik poslední změny ceny a předchozí cena je v PriceHistory s nejvyšším
	/// RecordedAt – ListingChangeProcessor tam při změně ukládá starou cenu se starým PriceFrom.
	/// Prázdná historie i neznámá cena vypadnou samy, v SQL je NULL &gt; x vždy NULL.
	/// Stejný predikát se používá i pro počet zlevnění, aby se číslo a seznam nemohly rozejít.
	/// </summary>
	internal static IQueryable<Listing> FilterPriceDropsSince(IQueryable<Listing> source, DateTimeOffset since)
	{
		return source.Where(x => x.RemovedAt == null
			&& x.Price != null
			&& x.PriceFrom >= since
			&& x.PriceHistories
				.OrderByDescending(h => h.RecordedAt)
				.ThenByDescending(h => h.Id)
				.Select(h => h.Price)
				.FirstOrDefault() > x.Price);
	}

	/// <summary>
	/// Poslední uzavřená cena inzerátu. Volá se až nad materializovaným výsledkem, který prošel
	/// <see cref="FilterPriceDropsSince"/> – tam už je zaručeno, že nějaká známá cena existuje.
	/// </summary>
	private static decimal GetPreviousPrice(Listing listing)
	{
		return listing.PriceHistories
			.Where(h => h.Price != null)
			.OrderByDescending(h => h.RecordedAt)
			.ThenByDescending(h => h.Id)
			.Select(h => h.Price!.Value)
			.First();
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