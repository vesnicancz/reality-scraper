using Microsoft.EntityFrameworkCore;
using RealityScraper.Domain.Entities.Realty;
using RealityScraper.Domain.Entities.Tasks;
using RealityScraper.Infrastructure.Contexts;

namespace RealityScraper.IntegrationTests.Database;

// Ověřuje, že se EF model poskládá z konfigurací v Infrastructure a sedí na migrace.
// Nepotřebuje běžící databázi – Npgsql provider model sestaví i bez připojení.
public class RealityDbContextModelTests
{
	private static RealityDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<RealityDbContext>()
			.UseNpgsql("Host=localhost;Database=reality_scraper;Username=test;Password=test")
			.Options;

		return new RealityDbContext(options);
	}

	[Theory]
	[InlineData(typeof(Listing))]
	[InlineData(typeof(PriceHistory))]
	[InlineData(typeof(ScraperTask))]
	[InlineData(typeof(ScraperTaskRecipient))]
	[InlineData(typeof(ScraperTaskTarget))]
	[InlineData(typeof(RemovedListingsReportTask))]
	[InlineData(typeof(ReportTaskRecipient))]
	[InlineData(typeof(ReportTaskSource))]
	public void Model_MapsEntity(Type entityType)
	{
		using var context = CreateContext();

		Assert.NotNull(context.Model.FindEntityType(entityType));
	}

	[Fact]
	public void Model_EveryEntityHasPrimaryKey()
	{
		using var context = CreateContext();

		var withoutKey = context.Model.GetEntityTypes()
			.Where(e => !e.IsOwned() && e.FindPrimaryKey() is null)
			.Select(e => e.Name)
			.ToList();

		Assert.Empty(withoutKey);
	}

	[Fact]
	public void Model_HasNoPendingChangesAgainstMigrations()
	{
		using var context = CreateContext();

		// Když tohle spadne, model se změnil a chybí k němu migrace
		// (dotnet ef migrations add ... v projektu Infrastructure).
		Assert.False(context.Database.HasPendingModelChanges());
	}
}
