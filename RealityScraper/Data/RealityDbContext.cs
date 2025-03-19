using Microsoft.EntityFrameworkCore;
using RealityScraper.Model;

namespace RealityScraper.Data;

// DbContext pro ukládání dat
public class RealityDbContext : DbContext
{
	public DbSet<Listing> Listings { get; set; }

	public RealityDbContext(DbContextOptions<RealityDbContext> options) : base(options)
	{
		// Automatické vytvoření databáze při prvním spuštění
		Database.EnsureCreated();
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// Konfigurace entity
		modelBuilder.Entity<Listing>()
			.HasIndex(e => e.ExternalId);
	}
}