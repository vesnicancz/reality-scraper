using Microsoft.EntityFrameworkCore;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.Infrastructure.Contexts;
using RealityScraper.Infrastructure.Repositories.Realty;

namespace RealityScraper.Infrastructure.Tests.Repositories.Realty;

// Ověřuje, že se dashboardový dotaz na zlevnění přeloží do SQL. ToQueryString() projde celým
// překladem a nepotřebuje běžící databázi - stejně jako RealityDbContextModelTests.
public class ListingRepositoryQueryTests
{
	private static readonly DateTimeOffset Since = new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);

	private static RealityDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<RealityDbContext>()
			.UseNpgsql("Host=localhost;Database=reality_scraper;Username=test;Password=test")
			.Options;

		return new RealityDbContext(options);
	}

	private static string GetPriceDropsSql()
	{
		using var context = CreateContext();

		return ListingRepository
			.FilterPriceDropsSince(context.Set<Listing>().AsNoTracking(), Since)
			.ToQueryString();
	}

	[Fact]
	public void FilterPriceDropsSince_TranslatesToSql()
	{
		var sql = GetPriceDropsSql();

		Assert.Contains("PriceHistory", sql, StringComparison.Ordinal);
	}

	[Fact]
	public void FilterPriceDropsSince_ComparesAgainstLastClosedPrice()
	{
		var sql = GetPriceDropsSql();

		// Předchozí cena se bere jako jeden řádek historie s nejvyšším RecordedAt.
		Assert.Contains("ORDER BY", sql, StringComparison.Ordinal);
		Assert.Contains("LIMIT 1", sql, StringComparison.Ordinal);
	}

	[Fact]
	public void FilterPriceDropsSince_CountTranslatesToSql()
	{
		using var context = CreateContext();

		// Stejný predikát se používá i pro počet zlevnění na dlaždici.
		var sql = ListingRepository
			.FilterPriceDropsSince(context.Set<Listing>().AsNoTracking(), Since)
			.Select(x => x.Id)
			.ToQueryString();

		Assert.Contains("PriceHistory", sql, StringComparison.Ordinal);
	}
}