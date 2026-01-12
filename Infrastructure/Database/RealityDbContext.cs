using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using RealityScraper.Application.Abstractions.Database;

namespace RealityScraper.Infrastructure.Contexts;

// DbContext pro ukládání dat
public class RealityDbContext : DbContext, IDbContext
{
	DbSet<T> IDbContext.Set<T>()
	{
		return base.Set<T>();
	}

	EntityEntry<TEntity> IDbContext.Entry<TEntity>(TEntity entity)
	{
		return base.Entry(entity);
	}

	Task<int> IDbContext.SaveChangesAsync(CancellationToken cancellationToken)
	{
		return base.SaveChangesAsync(cancellationToken);
	}

	public RealityDbContext(DbContextOptions<RealityDbContext> options)
		: base(options)
	{
		// Automatické vytvoření databáze při prvním spuštění
		Database.EnsureCreated();
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// Aplikace konfigurací entit
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(RealityDbContext).Assembly);

		base.OnModelCreating(modelBuilder);
	}
}